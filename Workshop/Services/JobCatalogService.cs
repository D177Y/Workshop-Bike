using Microsoft.EntityFrameworkCore;
using Workshop.Data;
using Workshop.Models;

namespace Workshop.Services;

public sealed class JobCatalogService
{
    private const int UnmappedCategorySortOrder = int.MaxValue / 4;

    private readonly DatabaseInitializer _initializer;
    private readonly IDbContextFactory<WorkshopDbContext> _factory;
    private readonly TenantContext _tenantContext;
    private bool _loaded;
    private int? _loadedTenantId;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private Dictionary<string, int> _category1Order = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, Dictionary<string, int>> _category2Order = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, int> _servicePackageOrderByName = new(StringComparer.OrdinalIgnoreCase);

    public JobCatalogService(DatabaseInitializer initializer, IDbContextFactory<WorkshopDbContext> factory, TenantContext tenantContext)
    {
        _initializer = initializer;
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public Dictionary<string, string> CategoryColors { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public IEnumerable<string> Categories => OrderCategories(CategoryColors.Keys);
    public Dictionary<string, List<string>> ServiceCategoryHierarchy { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> ServicePackageAddOnTimeReductions { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> SkillLevels { get; private set; } = new();
    public bool AutomaticServicePricingEnabled { get; set; }
    public decimal DefaultHourlyRate { get; set; } = 76m;
    public decimal DiscountedHourlyRate { get; set; } = 60m;
    public decimal LossLeaderHourlyRate { get; set; } = 50m;
    public decimal AutoPriceRoundingIncrement { get; set; } = 0.50m;
    public PriceRoundingMode AutoPriceRoundingMode { get; set; } = PriceRoundingMode.Down;
    public List<JobDefinition> Jobs { get; private set; } = new();
    public List<AddOnDefinition> AddOns { get; private set; } = new();
    public List<AddOnRule> Rules { get; private set; } = new();

    public async Task EnsureInitializedAsync()
    {
        var tenantId = _tenantContext.TenantId;
        if (_loaded && _loadedTenantId == tenantId) return;

        await _loadLock.WaitAsync();
        try
        {
            tenantId = _tenantContext.TenantId;
            if (_loaded && _loadedTenantId == tenantId) return;
            await _initializer.EnsureInitializedAsync();
            await ReloadAsync();
            _loaded = true;
            _loadedTenantId = tenantId;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public async Task ReloadAsync()
    {
        var tenantId = _tenantContext.TenantId;
        await using var db = await _factory.CreateDbContextAsync();

        var settings = await GetOrCreateCatalogSettingsAsync(db, tenantId);
        var globalCategories = await db.GlobalServiceCategories
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Category1)
            .ThenBy(c => c.Category2)
            .ToListAsync();
        var globalServicePackages = await db.GlobalServicePackageTemplates
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .ToListAsync();
        BuildCategoryOrderMaps(globalCategories, out _category1Order, out _category2Order);
        _servicePackageOrderByName = BuildServicePackageOrderMap(globalServicePackages);

        CategoryColors = BuildCategoryColors(settings);
        SkillLevels = BuildSkillLevels(settings);
        AutomaticServicePricingEnabled = settings.AutomaticServicePricingEnabled;
        DefaultHourlyRate = NormalizeHourlyRate(settings.DefaultHourlyRate, 76m);
        DiscountedHourlyRate = NormalizeHourlyRate(settings.DiscountedHourlyRate, 60m);
        LossLeaderHourlyRate = NormalizeHourlyRate(settings.LossLeaderHourlyRate, 50m);
        AutoPriceRoundingIncrement = NormalizeRoundingIncrement(settings.AutoPriceRoundingIncrement);
        AutoPriceRoundingMode = Enum.IsDefined(settings.AutoPriceRoundingMode)
            ? settings.AutoPriceRoundingMode
            : PriceRoundingMode.Down;
        ServicePackageAddOnTimeReductions = NormalizeServicePackageAddOnTimeReductions(settings.ServicePackageAddOnTimeReductions);

        var jobs = await db.JobDefinitions
            .Where(j => j.TenantId == tenantId)
            .ToListAsync();
        Jobs = OrderJobsForDisplay(jobs).ToList();

        foreach (var job in Jobs)
        {
            job.PackageOverrides = NormalizePackageOverrides(job.PackageOverrides);
            job.PackageChecklistItems = NormalizePackageChecklistItems(job.PackageChecklistItems);
        }

        ServiceCategoryHierarchy = BuildServiceCategoryHierarchy(settings, Jobs);

        AddOns = await db.AddOnDefinitions
            .Where(a => a.TenantId == tenantId)
            .OrderBy(a => a.Name)
            .ToListAsync();

        Rules = await db.AddOnRules
            .Where(r => r.TenantId == tenantId)
            .ToListAsync();
    }

    public bool IsServicePackage(JobDefinition job)
        => IsServicePackageCategory(job.Category);

    public bool IsServicePackage(string jobId)
        => Jobs.Any(j => j.Id.Equals(jobId, StringComparison.OrdinalIgnoreCase) && IsServicePackage(j));

    public bool RequiresManualQuoteAtUse(JobDefinition job)
    {
        if (job is null)
            return false;

        return job.PricingMode == ServicePricingMode.EstimatedPrice
            && job.BasePriceIncVat <= 0m
            && job.EstimatedPriceIncVat <= 0m;
    }

    public bool RequiresManualQuoteAtUse(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            return false;

        var job = Jobs.FirstOrDefault(j => j.Id.Equals(jobId, StringComparison.OrdinalIgnoreCase));
        return job is not null && RequiresManualQuoteAtUse(job);
    }

    public string? ResolveServicePackageId(IEnumerable<string>? jobIds, string? fallbackJobId = null)
    {
        if (jobIds is not null)
        {
            foreach (var jobId in jobIds)
            {
                if (IsServicePackage(jobId))
                    return jobId;
            }
        }

        if (!string.IsNullOrWhiteSpace(fallbackJobId) && IsServicePackage(fallbackJobId))
            return fallbackJobId;

        return null;
    }

    public (int minutes, decimal priceIncVat, string title) PriceAndTime(string jobId, string? servicePackageJobId = null)
    {
        var job = Jobs.First(x => x.Id == jobId);

        var minutes = job.DefaultMinutes;
        var price = ResolveDefaultPrice(job, minutes);

        if (!IsServicePackage(job) && !string.IsNullOrWhiteSpace(servicePackageJobId))
        {
            var match = job.PackageOverrides
                .FirstOrDefault(o => o.ServicePackageJobId.Equals(servicePackageJobId, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                if (!match.IsAvailableAsAdditionalService)
                {
                    minutes = 0;
                    price = 0m;
                }
                else
                {
                    minutes = Math.Max(0, match.Minutes);
                    price = Math.Max(0, match.PriceIncVat);
                }
            }
        }

        var title = job.Name;
        return (minutes, decimal.Round(price, 2, MidpointRounding.AwayFromZero), title);
    }

    public (int minutes, decimal priceIncVat, string title) PriceAndTime(string jobId, IReadOnlyCollection<string> _legacyAddOnIds)
        => PriceAndTime(jobId, (string?)null);

    public bool IsServiceAvailableAsAdditionalService(string serviceJobId, string? servicePackageJobId)
    {
        if (string.IsNullOrWhiteSpace(servicePackageJobId))
            return true;

        var job = Jobs.FirstOrDefault(x => x.Id.Equals(serviceJobId, StringComparison.OrdinalIgnoreCase));
        if (job is null || IsServicePackage(job))
            return false;

        var match = job.PackageOverrides
            .FirstOrDefault(o => o.ServicePackageJobId.Equals(servicePackageJobId, StringComparison.OrdinalIgnoreCase));

        return match?.IsAvailableAsAdditionalService ?? true;
    }

    public bool CanMechanicPerformJob(Mechanic mech, JobDefinition job)
    {
        var jobSkillLevel = NormalizeSkillLevel(job.SkillLevel);
        if (jobSkillLevel.Equals("All", StringComparison.OrdinalIgnoreCase))
            return true;

        var mechanicSkillLevel = NormalizeSkillLevel(mech.SkillLevel);
        if (mechanicSkillLevel.Equals("Custom", StringComparison.OrdinalIgnoreCase))
            return mech.CustomAllowedJobIds.Contains(job.Id);

        var mechIndex = SkillLevels.FindIndex(l => l.Equals(mechanicSkillLevel, StringComparison.OrdinalIgnoreCase));
        var jobIndex = SkillLevels.FindIndex(l => l.Equals(jobSkillLevel, StringComparison.OrdinalIgnoreCase));

        if (mechIndex < 0 || jobIndex < 0)
            return false;

        return mechIndex >= jobIndex;
    }

    public string ResolveJobColor(JobDefinition job)
    {
        if (!string.IsNullOrWhiteSpace(job.ColorHex))
            return job.ColorHex;

        if (CategoryColors.TryGetValue(job.Category, out var color))
            return color;

        return "#94a3b8";
    }

    public async Task SaveSettingsAsync()
    {
        await EnsureInitializedAsync();
        var tenantId = _tenantContext.TenantId;

        await using var db = await _factory.CreateDbContextAsync();
        var settings = await GetOrCreateCatalogSettingsAsync(db, tenantId);
        settings.CategoryColors = new Dictionary<string, string>(CategoryColors, StringComparer.OrdinalIgnoreCase);
        settings.SkillLevels = SkillLevels.ToList();
        settings.AutomaticServicePricingEnabled = AutomaticServicePricingEnabled;
        settings.DefaultHourlyRate = NormalizeHourlyRate(DefaultHourlyRate, 76m);
        settings.DiscountedHourlyRate = NormalizeHourlyRate(DiscountedHourlyRate, 60m);
        settings.LossLeaderHourlyRate = NormalizeHourlyRate(LossLeaderHourlyRate, 50m);
        settings.AutoPriceRoundingIncrement = NormalizeRoundingIncrement(AutoPriceRoundingIncrement);
        settings.AutoPriceRoundingMode = Enum.IsDefined(AutoPriceRoundingMode) ? AutoPriceRoundingMode : PriceRoundingMode.Down;
        settings.ServiceCategoryHierarchy = SortCategoryHierarchy(NormalizeCategoryHierarchy(ServiceCategoryHierarchy));
        settings.ServicePackageAddOnTimeReductions = NormalizeServicePackageAddOnTimeReductions(ServicePackageAddOnTimeReductions);
        await db.SaveChangesAsync();
    }

    public async Task SaveJobAsync(JobDefinition job)
    {
        await EnsureInitializedAsync();
        job.TenantId = _tenantContext.TenantId;
        job.Category = (job.Category ?? "").Trim();
        job.Category2 = (job.Category2 ?? "").Trim();
        if (!Enum.IsDefined(job.PricingMode))
            job.PricingMode = ServicePricingMode.FixedPrice;
        if (!Enum.IsDefined(job.AutoPricingTier))
            job.AutoPricingTier = ServiceHourlyRateTier.Default;
        job.BasePriceIncVat = decimal.Round(job.BasePriceIncVat, 2, MidpointRounding.AwayFromZero);
        job.EstimatedPriceIncVat = decimal.Round(Math.Max(0m, job.EstimatedPriceIncVat), 2, MidpointRounding.AwayFromZero);
        job.PackageOverrides = NormalizePackageOverrides(job.PackageOverrides);
        job.PackageChecklistItems = NormalizePackageChecklistItems(job.PackageChecklistItems);

        await using var db = await _factory.CreateDbContextAsync();
        var exists = await db.JobDefinitions.AnyAsync(x => x.TenantId == job.TenantId && x.Id == job.Id);
        if (exists)
        {
            db.Update(job);
        }
        else
        {
            db.Add(job);
        }
        if (!exists && !Jobs.Any(x => x.Id.Equals(job.Id, StringComparison.OrdinalIgnoreCase)))
        {
            Jobs.Add(job);
        }

        EnsureCategoryInSettings(job.Category, job.Category2);
        await db.SaveChangesAsync();
        Jobs = OrderJobsForDisplay(Jobs).ToList();
        ServiceCategoryHierarchy = SortCategoryHierarchy(ServiceCategoryHierarchy);
    }

    public async Task DeleteJobAsync(string jobId)
    {
        await EnsureInitializedAsync();
        var tenantId = _tenantContext.TenantId;

        await using var db = await _factory.CreateDbContextAsync();
        var job = await db.JobDefinitions.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == jobId);
        if (job is null) return;

        db.JobDefinitions.Remove(job);
        await db.SaveChangesAsync();
        Jobs.RemoveAll(j => j.Id == jobId);
    }

    public async Task SaveAddOnAsync(AddOnDefinition addOn)
    {
        await EnsureInitializedAsync();
        addOn.TenantId = _tenantContext.TenantId;

        await using var db = await _factory.CreateDbContextAsync();
        var exists = await db.AddOnDefinitions.AnyAsync(x => x.TenantId == addOn.TenantId && x.Id == addOn.Id);
        if (exists)
        {
            db.Update(addOn);
        }
        else
        {
            db.Add(addOn);
        }
        if (!exists)
        {
            AddOns.Add(addOn);
        }
        await db.SaveChangesAsync();
    }

    public async Task DeleteAddOnAsync(string addOnId)
    {
        await EnsureInitializedAsync();
        var tenantId = _tenantContext.TenantId;

        await using var db = await _factory.CreateDbContextAsync();
        var addOn = await db.AddOnDefinitions.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == addOnId);
        if (addOn is null) return;

        db.AddOnDefinitions.Remove(addOn);
        await db.SaveChangesAsync();
        AddOns.RemoveAll(a => a.Id == addOnId);
    }

    public async Task SaveRuleAsync(AddOnRule rule)
    {
        await EnsureInitializedAsync();
        rule.TenantId = _tenantContext.TenantId;

        await using var db = await _factory.CreateDbContextAsync();
        var exists = await db.AddOnRules.AnyAsync(x => x.TenantId == rule.TenantId && x.JobId == rule.JobId && x.AddOnId == rule.AddOnId);
        if (exists)
        {
            db.Update(rule);
        }
        else
        {
            db.Add(rule);
        }
        if (!exists)
        {
            Rules.Add(rule);
        }
        await db.SaveChangesAsync();
    }

    public async Task DeleteRuleAsync(string jobId, string addOnId)
    {
        await EnsureInitializedAsync();
        var tenantId = _tenantContext.TenantId;

        await using var db = await _factory.CreateDbContextAsync();
        var rule = await db.AddOnRules.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.JobId == jobId && x.AddOnId == addOnId);
        if (rule is null) return;

        db.AddOnRules.Remove(rule);
        await db.SaveChangesAsync();
        Rules.RemoveAll(r => r.JobId == jobId && r.AddOnId == addOnId);
    }

    private static Dictionary<string, string> BuildCategoryColors(CatalogSettings settings)
    {
        if (settings.CategoryColors.Count > 0)
            return new Dictionary<string, string>(settings.CategoryColors, StringComparer.OrdinalIgnoreCase);

        return new Dictionary<string, string>(
            SeedData.DefaultCatalogSettings(settings.TenantId).CategoryColors,
            StringComparer.OrdinalIgnoreCase);
    }

    private static List<string> BuildSkillLevels(CatalogSettings settings)
    {
        if (settings.SkillLevels.Count > 0)
        {
            var normalized = NormalizeSkillLevels(settings.SkillLevels);
            if (normalized.Count > 0)
                return normalized;
        }

        return NormalizeSkillLevels(SeedData.DefaultCatalogSettings(settings.TenantId).SkillLevels);
    }

    private static List<string> NormalizeSkillLevels(IEnumerable<string> levels)
        => levels
            .Select(NormalizeSkillLevel)
            .Where(level => !string.IsNullOrWhiteSpace(level))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static string NormalizeSkillLevel(string? skillLevel)
        => (skillLevel ?? "").Trim();

    private Dictionary<string, List<string>> BuildServiceCategoryHierarchy(CatalogSettings settings, IEnumerable<JobDefinition> jobs)
    {
        var hierarchy = NormalizeCategoryHierarchy(settings.ServiceCategoryHierarchy);
        foreach (var job in jobs)
        {
            var category1 = NormalizeCategory1(job.Category);
            var category2 = NormalizeCategory2(job.Category2);
            if (!hierarchy.TryGetValue(category1, out var category2List))
            {
                category2List = new List<string>();
                hierarchy[category1] = category2List;
            }

            if (string.IsNullOrWhiteSpace(category2))
                continue;

            if (category2List.Any(v => v.Equals(category2, StringComparison.OrdinalIgnoreCase)))
                continue;

            category2List.Add(category2);
        }

        return SortCategoryHierarchy(hierarchy);
    }

    public IReadOnlyList<string> GetCategory2Options(string category1)
    {
        var key = NormalizeCategory1(category1);
        if (ServiceCategoryHierarchy.TryGetValue(key, out var values))
            return OrderCategory2Options(key, values).ToList();

        return Array.Empty<string>();
    }

    public void ReplaceServiceCategoryHierarchy(Dictionary<string, List<string>> hierarchy)
    {
        ServiceCategoryHierarchy = SortCategoryHierarchy(NormalizeCategoryHierarchy(hierarchy));
    }

    public void ReplaceServicePackageAddOnTimeReductions(Dictionary<string, int> reductions)
    {
        ServicePackageAddOnTimeReductions = NormalizeServicePackageAddOnTimeReductions(reductions);
    }

    public decimal CalculatePriceForMinutes(JobDefinition job, int minutes)
        => ResolveDefaultPrice(job, Math.Max(0, minutes));

    private void EnsureCategoryInSettings(string category1, string category2)
    {
        var normalizedCategory1 = NormalizeCategory1(category1);
        var normalizedCategory2 = NormalizeCategory2(category2);
        if (!CategoryColors.ContainsKey(normalizedCategory1))
            CategoryColors[normalizedCategory1] = "#94a3b8";

        if (!ServiceCategoryHierarchy.TryGetValue(normalizedCategory1, out var category2List))
        {
            category2List = new List<string>();
            ServiceCategoryHierarchy[normalizedCategory1] = category2List;
        }

        if (!string.IsNullOrWhiteSpace(normalizedCategory2)
            && !category2List.Any(c => c.Equals(normalizedCategory2, StringComparison.OrdinalIgnoreCase)))
        {
            category2List.Add(normalizedCategory2);
            var ordered = OrderCategory2Options(normalizedCategory1, category2List).ToList();
            category2List.Clear();
            category2List.AddRange(ordered);
        }
    }

    private decimal ResolveDefaultPrice(JobDefinition job, int minutes)
    {
        switch (job.PricingMode)
        {
            case ServicePricingMode.AutoRate:
                if (!AutomaticServicePricingEnabled)
                    return decimal.Round(Math.Max(0, job.BasePriceIncVat), 2, MidpointRounding.AwayFromZero);

                var rate = job.AutoPricingTier switch
                {
                    ServiceHourlyRateTier.Discounted => DiscountedHourlyRate,
                    ServiceHourlyRateTier.LossLeader => LossLeaderHourlyRate,
                    _ => DefaultHourlyRate
                };
                return ApplyRounding(decimal.Round((Math.Max(0, minutes) / 60m) * rate, 2, MidpointRounding.AwayFromZero));
            case ServicePricingMode.EstimatedPrice:
                var estimate = job.EstimatedPriceIncVat > 0 ? job.EstimatedPriceIncVat : job.BasePriceIncVat;
                return ApplyRounding(decimal.Round(Math.Max(0, estimate), 2, MidpointRounding.AwayFromZero));
            case ServicePricingMode.FixedPrice:
            default:
                return ApplyRounding(decimal.Round(Math.Max(0, job.BasePriceIncVat), 2, MidpointRounding.AwayFromZero));
        }
    }

    private decimal ApplyRounding(decimal value)
    {
        var increment = NormalizeRoundingIncrement(AutoPriceRoundingIncrement);
        if (increment <= 0.01m)
            return decimal.Round(value, 2, MidpointRounding.AwayFromZero);

        var ratio = value / increment;
        var roundedRatio = AutoPriceRoundingMode switch
        {
            PriceRoundingMode.Up => decimal.Ceiling(ratio),
            PriceRoundingMode.Nearest => decimal.Round(ratio, 0, MidpointRounding.AwayFromZero),
            _ => decimal.Floor(ratio)
        };

        return decimal.Round(roundedRatio * increment, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal NormalizeHourlyRate(decimal value, decimal fallback)
    {
        var normalized = value > 0 ? value : fallback;
        return decimal.Round(normalized, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal NormalizeRoundingIncrement(decimal increment)
    {
        var supported = new[] { 0.01m, 0.25m, 0.50m, 1.00m };
        if (supported.Contains(increment))
            return increment;

        return 0.50m;
    }

    private static Dictionary<string, int> NormalizeServicePackageAddOnTimeReductions(Dictionary<string, int>? reductions)
    {
        var normalized = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (reductions is null)
            return normalized;

        foreach (var entry in reductions)
        {
            var packageId = (entry.Key ?? "").Trim();
            if (string.IsNullOrWhiteSpace(packageId))
                continue;

            normalized[packageId] = Math.Max(0, entry.Value);
        }

        return normalized;
    }

    private static Dictionary<string, List<string>> NormalizeCategoryHierarchy(Dictionary<string, List<string>>? hierarchy)
    {
        var normalized = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        if (hierarchy is null)
            return normalized;

        foreach (var entry in hierarchy)
        {
            var category1 = NormalizeCategory1(entry.Key);
            if (string.IsNullOrWhiteSpace(category1))
                continue;

            var category2Items = new List<string>();
            foreach (var rawValue in entry.Value ?? new List<string>())
            {
                var value = NormalizeCategory2(rawValue);
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                if (category2Items.Any(existing => existing.Equals(value, StringComparison.OrdinalIgnoreCase)))
                    continue;

                category2Items.Add(value);
            }

            normalized[category1] = category2Items;
        }

        return normalized;
    }

    public int GetCategory1SortOrder(string? category1)
    {
        var normalized = NormalizeCategory1(category1);
        return _category1Order.TryGetValue(normalized, out var order)
            ? order
            : UnmappedCategorySortOrder;
    }

    public int GetCategory2SortOrder(string? category1, string? category2)
    {
        var normalizedCategory2 = NormalizeCategory2(category2);
        if (string.IsNullOrWhiteSpace(normalizedCategory2))
            return -1;

        var normalizedCategory1 = NormalizeCategory1(category1);
        if (_category2Order.TryGetValue(normalizedCategory1, out var map)
            && map.TryGetValue(normalizedCategory2, out var order))
        {
            return order;
        }

        return UnmappedCategorySortOrder;
    }

    public int CompareCategory1(string? leftCategory1, string? rightCategory1)
    {
        var left = NormalizeCategory1(leftCategory1);
        var right = NormalizeCategory1(rightCategory1);
        var byOrder = GetCategory1SortOrder(left).CompareTo(GetCategory1SortOrder(right));
        if (byOrder != 0)
            return byOrder;

        return StringComparer.OrdinalIgnoreCase.Compare(left, right);
    }

    public int CompareCategory2(string? category1, string? leftCategory2, string? rightCategory2)
    {
        var left = NormalizeCategory2(leftCategory2);
        var right = NormalizeCategory2(rightCategory2);
        var byOrder = GetCategory2SortOrder(category1, left).CompareTo(GetCategory2SortOrder(category1, right));
        if (byOrder != 0)
            return byOrder;

        return StringComparer.OrdinalIgnoreCase.Compare(left, right);
    }

    public IEnumerable<string> OrderCategories(IEnumerable<string> categories, bool servicePackagesFirst = false)
    {
        var normalized = (categories ?? Enumerable.Empty<string>())
            .Select(NormalizeCategory1)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => GetCategory1SortOrder(value))
            .ThenBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!servicePackagesFirst)
            return normalized;

        var packageCategory = normalized.FirstOrDefault(IsServicePackageCategory);
        if (string.IsNullOrWhiteSpace(packageCategory))
            return normalized;

        return new[] { packageCategory }
            .Concat(normalized.Where(value => !value.Equals(packageCategory, StringComparison.OrdinalIgnoreCase)));
    }

    public IEnumerable<string> OrderCategory2Options(string category1, IEnumerable<string> category2Options)
    {
        var normalizedCategory1 = NormalizeCategory1(category1);
        return (category2Options ?? Enumerable.Empty<string>())
            .Select(NormalizeCategory2)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => GetCategory2SortOrder(normalizedCategory1, value))
            .ThenBy(value => value, StringComparer.OrdinalIgnoreCase);
    }

    public IEnumerable<JobDefinition> OrderJobsForDisplay(IEnumerable<JobDefinition> jobs, bool servicePackagesFirst = false)
    {
        var source = jobs ?? Enumerable.Empty<JobDefinition>();
        var ordered = source
            .OrderBy(job => servicePackagesFirst && IsServicePackage(job) ? 0 : 1)
            .ThenBy(job => GetCategory1SortOrder(job.Category))
            .ThenBy(job => NormalizeCategory1(job.Category), StringComparer.OrdinalIgnoreCase)
            .ThenBy(job => GetCategory2SortOrder(job.Category, job.Category2))
            .ThenBy(job => NormalizeCategory2(job.Category2), StringComparer.OrdinalIgnoreCase)
            .ThenBy(GetServicePackageSortOrder)
            .ThenBy(job => (job.Name ?? "").Trim(), StringComparer.OrdinalIgnoreCase);

        return ordered;
    }

    private int GetServicePackageSortOrder(JobDefinition job)
    {
        if (!IsServicePackage(job))
            return UnmappedCategorySortOrder;

        var normalizedName = NormalizeServicePackageName(job.Name);
        if (string.IsNullOrWhiteSpace(normalizedName))
            return UnmappedCategorySortOrder;

        return _servicePackageOrderByName.TryGetValue(normalizedName, out var order)
            ? order
            : UnmappedCategorySortOrder;
    }

    private static Dictionary<string, int> BuildServicePackageOrderMap(IEnumerable<GlobalServicePackageTemplate> packages)
    {
        var orderByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var nextOrder = 0;
        foreach (var package in packages ?? Enumerable.Empty<GlobalServicePackageTemplate>())
        {
            var normalizedName = NormalizeServicePackageName(package.Name);
            if (string.IsNullOrWhiteSpace(normalizedName) || orderByName.ContainsKey(normalizedName))
                continue;

            orderByName[normalizedName] = nextOrder++;
        }

        return orderByName;
    }

    private Dictionary<string, List<string>> SortCategoryHierarchy(Dictionary<string, List<string>> hierarchy)
    {
        var sorted = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var category1 in OrderCategories(hierarchy.Keys))
        {
            hierarchy.TryGetValue(category1, out var category2Items);
            sorted[category1] = OrderCategory2Options(category1, category2Items ?? new List<string>()).ToList();
        }

        return sorted;
    }

    private static bool IsServicePackageCategory(string? category)
        => string.Equals((category ?? "").Trim(), "Service Packages", StringComparison.OrdinalIgnoreCase);

    private static void BuildCategoryOrderMaps(
        IEnumerable<GlobalServiceCategory> categories,
        out Dictionary<string, int> category1Order,
        out Dictionary<string, Dictionary<string, int>> category2Order)
    {
        category1Order = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        category2Order = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
        var nextCategory1 = 0;
        var orderedRows = (categories ?? Enumerable.Empty<GlobalServiceCategory>())
            .OrderBy(row => row.SortOrder)
            .ThenBy(row => NormalizeCategory1(row.Category1), StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => NormalizeCategory2(row.Category2), StringComparer.OrdinalIgnoreCase)
            .ToList();
        var rootRows = orderedRows
            .Where(row => string.IsNullOrWhiteSpace((row.Category2 ?? "").Trim()))
            .ToList();
        var subRows = orderedRows
            .Where(row => !string.IsNullOrWhiteSpace((row.Category2 ?? "").Trim()))
            .ToList();

        foreach (var root in rootRows)
        {
            var category1 = NormalizeCategory1(root.Category1);
            if (category1Order.ContainsKey(category1))
                continue;

            category1Order[category1] = nextCategory1++;
        }

        foreach (var sub in subRows)
        {
            var category1 = NormalizeCategory1(sub.Category1);
            if (!category1Order.ContainsKey(category1))
                category1Order[category1] = nextCategory1++;
        }

        var category1OrderMap = category1Order;
        foreach (var sub in subRows
                     .OrderBy(row => category1OrderMap.TryGetValue(NormalizeCategory1(row.Category1), out var order) ? order : UnmappedCategorySortOrder)
                     .ThenBy(row => row.SortOrder)
                     .ThenBy(row => NormalizeCategory2(row.Category2), StringComparer.OrdinalIgnoreCase))
        {
            var category1 = NormalizeCategory1(sub.Category1);
            var category2 = NormalizeCategory2(sub.Category2);
            if (!category2Order.TryGetValue(category1, out var map))
            {
                map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                category2Order[category1] = map;
            }

            if (map.ContainsKey(category2))
                continue;

            map[category2] = map.Count;
        }
    }

    private static string NormalizeCategory1(string? category)
    {
        var value = (category ?? "").Trim();
        return string.IsNullOrWhiteSpace(value) ? "Uncategorized" : value;
    }

    private static string NormalizeServicePackageName(string? packageName)
        => (packageName ?? "").Trim();

    private static string NormalizeCategory2(string? category)
        => (category ?? "").Trim();

    private static async Task<CatalogSettings> GetOrCreateCatalogSettingsAsync(WorkshopDbContext db, int tenantId)
    {
        var settings = await db.CatalogSettings.FirstOrDefaultAsync(x => x.TenantId == tenantId);
        if (settings is not null)
            return settings;

        settings = SeedData.DefaultCatalogSettings(tenantId);
        db.CatalogSettings.Add(settings);

        try
        {
            await db.SaveChangesAsync();
            return settings;
        }
        catch (DbUpdateException)
        {
            db.Entry(settings).State = EntityState.Detached;
            return await db.CatalogSettings.FirstAsync(x => x.TenantId == tenantId);
        }
    }

    private static List<JobServicePackageOverride> NormalizePackageOverrides(IEnumerable<JobServicePackageOverride>? overrides)
    {
        var normalized = new Dictionary<string, JobServicePackageOverride>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in overrides ?? Enumerable.Empty<JobServicePackageOverride>())
        {
            var packageId = (entry.ServicePackageJobId ?? "").Trim();
            if (string.IsNullOrWhiteSpace(packageId))
                continue;

            normalized[packageId] = new JobServicePackageOverride
            {
                ServicePackageJobId = packageId,
                IsAvailableAsAdditionalService = entry.IsAvailableAsAdditionalService,
                Minutes = entry.IsAvailableAsAdditionalService ? Math.Max(0, entry.Minutes) : 0,
                PriceIncVat = entry.IsAvailableAsAdditionalService
                    ? decimal.Round(Math.Max(0, entry.PriceIncVat), 2, MidpointRounding.AwayFromZero)
                    : 0m
            };
        }

        return normalized.Values
            .OrderBy(o => o.ServicePackageJobId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<ServicePackageChecklistItemDefinition> NormalizePackageChecklistItems(IEnumerable<ServicePackageChecklistItemDefinition>? items)
    {
        var normalized = new List<ServicePackageChecklistItemDefinition>();
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items ?? Enumerable.Empty<ServicePackageChecklistItemDefinition>())
        {
            var linkedServiceJobId = (item.LinkedServiceJobId ?? "").Trim();
            var name = (item.Name ?? "").Trim();
            var description = (item.Description ?? "").Trim();
            if (string.IsNullOrWhiteSpace(linkedServiceJobId) && string.IsNullOrWhiteSpace(name))
                continue;

            var id = (item.Id ?? "").Trim();
            if (string.IsNullOrWhiteSpace(id) || !seenIds.Add(id))
                id = Guid.NewGuid().ToString("N");

            normalized.Add(new ServicePackageChecklistItemDefinition
            {
                Id = id,
                SortOrder = Math.Max(0, item.SortOrder),
                Name = name,
                Description = description,
                LinkedServiceJobId = linkedServiceJobId
            });
        }

        return normalized
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .Select((item, index) =>
            {
                item.SortOrder = index;
                return item;
            })
            .ToList();
    }
}
