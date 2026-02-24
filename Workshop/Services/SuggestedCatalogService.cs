using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Workshop.Data;
using Workshop.Models;

namespace Workshop.Services;

public sealed class SuggestedCatalogService
{
    private const string ServicePackagesCategoryUpper = "SERVICE PACKAGES";
    private const string ServicePackagesCategory = "Service Packages";

    private readonly IDbContextFactory<WorkshopDbContext> _dbFactory;

    public SuggestedCatalogService(IDbContextFactory<WorkshopDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<GlobalServiceCategory>> GetGlobalCategoriesAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.GlobalServiceCategories
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Category1)
            .ThenBy(c => c.Category2)
            .ToListAsync();
    }

    public async Task SaveGlobalCategoryAsync(GlobalServiceCategory category)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        category.Category1 = NormalizeCategory1(category.Category1);
        category.Category2 = NormalizeCategory2(category.Category2);
        category.ColorHex = NormalizeColor(category.ColorHex);

        var exists = category.Id > 0 && await db.GlobalServiceCategories.AnyAsync(c => c.Id == category.Id);
        if (exists)
            db.Update(category);
        else
            db.Add(category);

        await db.SaveChangesAsync();
    }

    public async Task DeleteGlobalCategoryAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var category = await db.GlobalServiceCategories.FirstOrDefaultAsync(c => c.Id == id);
        if (category is null)
            return;

        db.GlobalServiceCategories.Remove(category);
        await db.SaveChangesAsync();
    }

    public async Task<List<GlobalServiceTemplate>> GetGlobalTemplatesAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.GlobalServiceTemplates
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Category1)
            .ThenBy(s => s.Category2)
            .ThenBy(s => s.Name)
            .ToListAsync();
    }

    public async Task SaveGlobalTemplateAsync(GlobalServiceTemplate template)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        NormalizeTemplate(template);

        var exists = template.Id > 0 && await db.GlobalServiceTemplates.AnyAsync(s => s.Id == template.Id);
        if (exists)
            db.Update(template);
        else
            db.Add(template);

        await db.SaveChangesAsync();
    }

    public async Task DeleteGlobalTemplateAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var template = await db.GlobalServiceTemplates.FirstOrDefaultAsync(s => s.Id == id);
        if (template is null)
            return;

        db.GlobalServiceTemplates.Remove(template);
        await db.SaveChangesAsync();
    }

    public async Task<List<GlobalServicePackageTemplate>> GetGlobalServicePackageTemplatesAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var packages = await db.GlobalServicePackageTemplates
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .ToListAsync();

        foreach (var package in packages)
            NormalizeGlobalPackageTemplate(package);

        return packages;
    }

    public async Task SaveGlobalServicePackageTemplateAsync(GlobalServicePackageTemplate template)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        NormalizeGlobalPackageTemplate(template);

        if (template.Id > 0)
        {
            template.Items = NormalizeGlobalPackageItems(
                template.Items.Where(i => i.IncludedGlobalServicePackageTemplateId != template.Id));
        }

        var targetNameUpper = template.Name.ToUpperInvariant();
        var duplicateNameExists = await db.GlobalServicePackageTemplates
            .AnyAsync(p => p.Id != template.Id && (p.Name ?? "").ToUpper() == targetNameUpper);

        if (duplicateNameExists)
            throw new InvalidOperationException("A service package with this name already exists.");

        var existing = template.Id > 0
            ? await db.GlobalServicePackageTemplates.FirstOrDefaultAsync(p => p.Id == template.Id)
            : null;

        if (existing is null)
        {
            var toCreate = new GlobalServicePackageTemplate
            {
                Name = template.Name,
                SkillLevel = template.SkillLevel,
                Description = template.Description,
                DefaultMinutes = template.DefaultMinutes,
                BasePriceIncVat = template.BasePriceIncVat,
                PricingMode = template.PricingMode,
                AutoPricingTier = template.AutoPricingTier,
                EstimatedPriceIncVat = template.EstimatedPriceIncVat,
                SortOrder = Math.Max(0, template.SortOrder),
                Items = template.Items.Select(CloneGlobalPackageItem).ToList()
            };

            db.GlobalServicePackageTemplates.Add(toCreate);
        }
        else
        {
            existing.Name = template.Name;
            existing.SkillLevel = template.SkillLevel;
            existing.Description = template.Description;
            existing.DefaultMinutes = template.DefaultMinutes;
            existing.BasePriceIncVat = template.BasePriceIncVat;
            existing.PricingMode = template.PricingMode;
            existing.AutoPricingTier = template.AutoPricingTier;
            existing.EstimatedPriceIncVat = template.EstimatedPriceIncVat;
            existing.SortOrder = Math.Max(0, template.SortOrder);
            existing.Items = template.Items.Select(CloneGlobalPackageItem).ToList();
        }

        await db.SaveChangesAsync();
    }

    public async Task DeleteGlobalServicePackageTemplateAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var template = await db.GlobalServicePackageTemplates.FirstOrDefaultAsync(p => p.Id == id);
        if (template is null)
            return;

        db.GlobalServicePackageTemplates.Remove(template);

        var templates = await db.GlobalServicePackageTemplates
            .Where(p => p.Id != id)
            .ToListAsync();

        foreach (var package in templates)
        {
            var items = package.Items ?? new List<GlobalServicePackageItemDefinition>();
            if (!items.Any(i => i.IncludedGlobalServicePackageTemplateId == id))
                continue;

            package.Items = NormalizeGlobalPackageItems(
                items.Where(i => i.IncludedGlobalServicePackageTemplateId != id));
        }

        await db.SaveChangesAsync();
    }

    public async Task<SuggestedImportResult> ImportSuggestedAsync(int tenantId, SuggestedImportMode mode)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var result = new SuggestedImportResult();

        var settings = await LoadOrCreateCatalogSettingsAsync(db, tenantId);
        var globalCategories = await db.GlobalServiceCategories
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Category1)
            .ThenBy(c => c.Category2)
            .ToListAsync();

        var globalServices = await db.GlobalServiceTemplates
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Category1)
            .ThenBy(s => s.Category2)
            .ThenBy(s => s.Name)
            .ToListAsync();

        var globalServicePackages = await db.GlobalServicePackageTemplates
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .ToListAsync();

        var doCategories = mode is SuggestedImportMode.CategoriesOnly or SuggestedImportMode.ServicesOnly or SuggestedImportMode.ServicesAndTimes or SuggestedImportMode.FullSetup;
        var doServices = mode is SuggestedImportMode.ServicesOnly or SuggestedImportMode.ServicesAndTimes or SuggestedImportMode.ServicePrices or SuggestedImportMode.FullSetup;
        var includeTimes = mode is SuggestedImportMode.ServicesAndTimes or SuggestedImportMode.FullSetup;
        var includePrices = mode is SuggestedImportMode.ServicePrices or SuggestedImportMode.FullSetup;

        if (doCategories)
            ApplyCategories(settings, globalCategories, result);

        if (doServices)
        {
            await ApplyServicesAsync(db, tenantId, globalServices, includeTimes, includePrices, result);
            await ApplyServicePackagesAsync(db, tenantId, globalServicePackages, globalServices, includeTimes, includePrices, result);
            if (includeTimes && includePrices)
            {
                await ApplyDefaultServicePackageOverridesAsync(db, tenantId, globalServices, globalServicePackages);
            }
            await ApplyDefaultServicePackageAddOnReductionsAsync(db, tenantId, settings, globalServicePackages);
        }

        await db.SaveChangesAsync();
        return result;
    }

    private static void ApplyCategories(CatalogSettings settings, List<GlobalServiceCategory> globalCategories, SuggestedImportResult result)
    {
        var colors = settings.CategoryColors ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var hierarchy = settings.ServiceCategoryHierarchy ?? new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var category in globalCategories)
        {
            var category1 = NormalizeCategory1(category.Category1);
            var category2 = NormalizeCategory2(category.Category2);
            var color = NormalizeColor(category.ColorHex);

            if (!colors.ContainsKey(category1))
            {
                colors[category1] = color;
                result.CategoriesAdded++;
            }
            else if (!string.Equals(colors[category1], color, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(color))
            {
                colors[category1] = color;
                result.CategoriesUpdated++;
            }

            if (!hierarchy.TryGetValue(category1, out var category2List))
            {
                category2List = new List<string>();
                hierarchy[category1] = category2List;
            }

            if (!string.IsNullOrWhiteSpace(category2)
                && !category2List.Any(c => c.Equals(category2, StringComparison.OrdinalIgnoreCase)))
            {
                category2List.Add(category2);
                category2List.Sort(StringComparer.OrdinalIgnoreCase);
                result.CategoriesUpdated++;
            }
        }

        settings.CategoryColors = new Dictionary<string, string>(colors, StringComparer.OrdinalIgnoreCase);
        settings.ServiceCategoryHierarchy = hierarchy
            .ToDictionary(
                kvp => NormalizeCategory1(kvp.Key),
                kvp => (kvp.Value ?? new List<string>())
                    .Select(NormalizeCategory2)
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                StringComparer.OrdinalIgnoreCase);
    }

    private static async Task ApplyServicesAsync(
        WorkshopDbContext db,
        int tenantId,
        List<GlobalServiceTemplate> globalServices,
        bool includeTimes,
        bool includePrices,
        SuggestedImportResult result)
    {
        var tenantServices = await db.JobDefinitions
            .Where(j => j.TenantId == tenantId && (j.Category ?? "").ToUpper() != ServicePackagesCategoryUpper)
            .ToListAsync();

        var existingByKey = tenantServices.ToDictionary(
            BuildServiceKey,
            StringComparer.OrdinalIgnoreCase);

        var nextIndex = tenantServices
            .Select(s => ParseJobIndex(s.Id))
            .DefaultIfEmpty(0)
            .Max();

        foreach (var template in globalServices)
        {
            NormalizeTemplate(template);
            var key = BuildServiceKey(template.Name, template.Category1, template.Category2);
            if (!existingByKey.TryGetValue(key, out var existing))
            {
                var service = new JobDefinition
                {
                    TenantId = tenantId,
                    Id = NextJobId(++nextIndex),
                    Name = template.Name,
                    PartNumber = template.PartNumber,
                    Category = template.Category1,
                    Category2 = template.Category2,
                    SkillLevel = string.IsNullOrWhiteSpace(template.SkillLevel) ? "All" : template.SkillLevel,
                    Description = template.Description,
                    DefaultMinutes = includeTimes ? ResolveTemplateMinutes(template) : 30,
                    BasePriceIncVat = includePrices ? RoundPrice(template.BasePriceIncVat) : 0m,
                    PricingMode = includePrices ? template.PricingMode : ServicePricingMode.FixedPrice,
                    AutoPricingTier = includePrices ? template.AutoPricingTier : ServiceHourlyRateTier.Default,
                    EstimatedPriceIncVat = includePrices ? RoundPrice(template.EstimatedPriceIncVat) : 0m
                };

                db.JobDefinitions.Add(service);
                existingByKey[key] = service;
                result.ServicesAdded++;
                continue;
            }

            var changed = false;
            if (includeTimes)
            {
                var targetMinutes = ResolveTemplateMinutes(template);
                if (existing.DefaultMinutes != targetMinutes)
                {
                    existing.DefaultMinutes = targetMinutes;
                    changed = true;
                }
            }

            if (includePrices)
            {
                var targetBase = RoundPrice(template.BasePriceIncVat);
                var targetEstimated = RoundPrice(template.EstimatedPriceIncVat);
                if (existing.BasePriceIncVat != targetBase)
                {
                    existing.BasePriceIncVat = targetBase;
                    changed = true;
                }

                if (existing.PricingMode != template.PricingMode)
                {
                    existing.PricingMode = template.PricingMode;
                    changed = true;
                }

                if (existing.AutoPricingTier != template.AutoPricingTier)
                {
                    existing.AutoPricingTier = template.AutoPricingTier;
                    changed = true;
                }

                if (existing.EstimatedPriceIncVat != targetEstimated)
                {
                    existing.EstimatedPriceIncVat = targetEstimated;
                    changed = true;
                }
            }

            if (changed)
            {
                db.JobDefinitions.Update(existing);
                result.ServicesUpdated++;
            }
        }
    }

    private static async Task ApplyServicePackagesAsync(
        WorkshopDbContext db,
        int tenantId,
        List<GlobalServicePackageTemplate> globalPackages,
        List<GlobalServiceTemplate> globalServices,
        bool includeTimes,
        bool includePrices,
        SuggestedImportResult result)
    {
        if (globalPackages.Count == 0)
            return;

        var tenantJobs = await db.JobDefinitions
            .Where(j => j.TenantId == tenantId)
            .ToListAsync();

        var tenantServicesByKey = tenantJobs
            .Where(j => (j.Category ?? "").ToUpper() != ServicePackagesCategoryUpper)
            .GroupBy(BuildServiceKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var tenantPackageJobs = tenantJobs
            .Where(j => (j.Category ?? "").ToUpper() == ServicePackagesCategoryUpper)
            .ToList();

        var existingByPackageName = tenantPackageJobs
            .GroupBy(j => (j.Name ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var globalServicesById = globalServices.ToDictionary(s => s.Id);
        var tenantServiceByTemplateId = new Dictionary<int, JobDefinition>();
        foreach (var template in globalServices)
        {
            NormalizeTemplate(template);
            var key = BuildServiceKey(template.Name, template.Category1, template.Category2);
            if (tenantServicesByKey.TryGetValue(key, out var tenantService))
                tenantServiceByTemplateId[template.Id] = tenantService;
        }

        var globalPackagesById = globalPackages.ToDictionary(p => p.Id);
        var usedJobIds = tenantJobs
            .Select(j => (j.Id ?? "").Trim())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var package in globalPackages
                     .OrderBy(p => p.SortOrder)
                     .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase))
        {
            NormalizeGlobalPackageTemplate(package);
            if (!existingByPackageName.TryGetValue(package.Name, out var existing))
            {
                var created = new JobDefinition
                {
                    TenantId = tenantId,
                    Id = NextServicePackageJobId(package.Name, usedJobIds),
                    Name = package.Name,
                    Category = ServicePackagesCategory,
                    Category2 = "",
                    SkillLevel = string.IsNullOrWhiteSpace(package.SkillLevel) ? "All" : package.SkillLevel,
                    Description = package.Description,
                    DefaultMinutes = includeTimes ? ResolveGlobalPackageMinutes(package) : 60,
                    BasePriceIncVat = includePrices ? RoundPrice(package.BasePriceIncVat) : 0m,
                    PricingMode = includePrices ? package.PricingMode : ServicePricingMode.FixedPrice,
                    AutoPricingTier = includePrices ? package.AutoPricingTier : ServiceHourlyRateTier.Default,
                    EstimatedPriceIncVat = includePrices ? RoundPrice(package.EstimatedPriceIncVat) : 0m,
                    PackageChecklistItems = BuildResolvedPackageChecklistItems(
                        package,
                        globalPackagesById,
                        globalServicesById,
                        tenantServiceByTemplateId)
                };

                db.JobDefinitions.Add(created);
                existingByPackageName[package.Name] = created;
                result.ServicesAdded++;
                continue;
            }

            var changed = false;
            if (!string.Equals(existing.Name, package.Name, StringComparison.Ordinal))
            {
                existing.Name = package.Name;
                changed = true;
            }

            if (!string.Equals(existing.Category, ServicePackagesCategory, StringComparison.OrdinalIgnoreCase))
            {
                existing.Category = ServicePackagesCategory;
                changed = true;
            }

            if (!string.IsNullOrWhiteSpace((existing.Category2 ?? "").Trim()))
            {
                existing.Category2 = "";
                changed = true;
            }

            var normalizedSkill = string.IsNullOrWhiteSpace(package.SkillLevel) ? "All" : package.SkillLevel;
            if (!string.Equals(existing.SkillLevel, normalizedSkill, StringComparison.Ordinal))
            {
                existing.SkillLevel = normalizedSkill;
                changed = true;
            }

            if (!string.Equals((existing.Description ?? "").Trim(), package.Description, StringComparison.Ordinal))
            {
                existing.Description = package.Description;
                changed = true;
            }

            if (includeTimes)
            {
                var minutes = ResolveGlobalPackageMinutes(package);
                if (existing.DefaultMinutes != minutes)
                {
                    existing.DefaultMinutes = minutes;
                    changed = true;
                }
            }

            if (includePrices)
            {
                var targetBase = RoundPrice(package.BasePriceIncVat);
                var targetEstimated = RoundPrice(package.EstimatedPriceIncVat);
                if (existing.BasePriceIncVat != targetBase)
                {
                    existing.BasePriceIncVat = targetBase;
                    changed = true;
                }

                if (existing.PricingMode != package.PricingMode)
                {
                    existing.PricingMode = package.PricingMode;
                    changed = true;
                }

                if (existing.AutoPricingTier != package.AutoPricingTier)
                {
                    existing.AutoPricingTier = package.AutoPricingTier;
                    changed = true;
                }

                if (existing.EstimatedPriceIncVat != targetEstimated)
                {
                    existing.EstimatedPriceIncVat = targetEstimated;
                    changed = true;
                }
            }

            var resolvedChecklist = BuildResolvedPackageChecklistItems(
                package,
                globalPackagesById,
                globalServicesById,
                tenantServiceByTemplateId);

            if (!PackageChecklistEquivalent(existing.PackageChecklistItems, resolvedChecklist))
            {
                existing.PackageChecklistItems = resolvedChecklist;
                changed = true;
            }

            if (!changed)
                continue;

            db.JobDefinitions.Update(existing);
            result.ServicesUpdated++;
        }
    }

    private static async Task ApplyDefaultServicePackageAddOnReductionsAsync(
        WorkshopDbContext db,
        int tenantId,
        CatalogSettings tenantSettings,
        IReadOnlyCollection<GlobalServicePackageTemplate> globalPackages)
    {
        var globalSettings = await LoadOrCreateCatalogSettingsAsync(db, 0);
        var globalReductionByPackageName = ResolveGlobalDefaultPackageReductionsByName(
            globalSettings.ServicePackageAddOnTimeReductions,
            globalPackages);

        var tenantPackageJobs = await db.JobDefinitions
            .Where(j => j.TenantId == tenantId && (j.Category ?? "").ToUpper() == ServicePackagesCategoryUpper)
            .ToListAsync();

        var tenantPackageByName = tenantPackageJobs
            .Where(j => !string.IsNullOrWhiteSpace((j.Id ?? "").Trim()))
            .GroupBy(j => ServicePackageReductionKeyHelper.NormalizePackageName(j.Name), StringComparer.OrdinalIgnoreCase)
            .Where(group => !string.IsNullOrWhiteSpace(group.Key))
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var mapped = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var package in globalPackages)
        {
            var packageName = ServicePackageReductionKeyHelper.NormalizePackageName(package.Name);
            if (string.IsNullOrWhiteSpace(packageName))
                continue;

            if (!tenantPackageByName.TryGetValue(packageName, out var tenantPackage))
                continue;

            var packageId = (tenantPackage.Id ?? "").Trim();
            if (string.IsNullOrWhiteSpace(packageId))
                continue;

            var reduction = globalReductionByPackageName.TryGetValue(packageName, out var value)
                ? value
                : 0;
            mapped[packageId] = Math.Max(0, reduction);
        }

        tenantSettings.ServicePackageAddOnTimeReductions = mapped;
    }

    private static async Task ApplyDefaultServicePackageOverridesAsync(
        WorkshopDbContext db,
        int tenantId,
        IReadOnlyCollection<GlobalServiceTemplate> globalServices,
        IReadOnlyCollection<GlobalServicePackageTemplate> globalPackages)
    {
        if (globalServices.Count == 0 || globalPackages.Count == 0)
            return;

        var globalSettings = await LoadOrCreateCatalogSettingsAsync(db, 0);
        var globalReductionByPackageName = ResolveGlobalDefaultPackageReductionsByName(
            globalSettings.ServicePackageAddOnTimeReductions,
            globalPackages);

        var tenantJobs = await db.JobDefinitions
            .Where(j => j.TenantId == tenantId)
            .ToListAsync();

        var tenantServicesByKey = tenantJobs
            .Where(j => (j.Category ?? "").ToUpper() != ServicePackagesCategoryUpper)
            .GroupBy(BuildServiceKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var tenantPackagesByName = tenantJobs
            .Where(j => (j.Category ?? "").ToUpper() == ServicePackagesCategoryUpper)
            .Where(j => !string.IsNullOrWhiteSpace((j.Id ?? "").Trim()))
            .GroupBy(j => ServicePackageReductionKeyHelper.NormalizePackageName(j.Name), StringComparer.OrdinalIgnoreCase)
            .Where(group => !string.IsNullOrWhiteSpace(group.Key))
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var packageContexts = globalPackages
            .Select(package =>
            {
                var packageName = ServicePackageReductionKeyHelper.NormalizePackageName(package.Name);
                if (string.IsNullOrWhiteSpace(packageName))
                    return null;

                if (!tenantPackagesByName.TryGetValue(packageName, out var tenantPackage))
                    return null;

                var tenantPackageJobId = (tenantPackage.Id ?? "").Trim();
                if (string.IsNullOrWhiteSpace(tenantPackageJobId))
                    return null;

                var reduction = globalReductionByPackageName.TryGetValue(packageName, out var value)
                    ? Math.Max(0, value)
                    : 0;

                return new ImportPackageOverrideContext(
                    package.Id,
                    tenantPackageJobId,
                    reduction);
            })
            .Where(context => context is not null)
            .Select(context => context!)
            .OrderBy(context => context.GlobalServicePackageTemplateId)
            .ToList();

        if (packageContexts.Count == 0)
            return;

        var pricingDefaults = new GlobalDefaultPricingContext(
            NormalizeHourlyRate(globalSettings.DefaultHourlyRate, 76m),
            NormalizeHourlyRate(globalSettings.DiscountedHourlyRate, 60m),
            NormalizeHourlyRate(globalSettings.LossLeaderHourlyRate, 50m),
            NormalizeRoundingIncrement(globalSettings.AutoPriceRoundingIncrement),
            Enum.IsDefined(globalSettings.AutoPriceRoundingMode)
                ? globalSettings.AutoPriceRoundingMode
                : PriceRoundingMode.Down);

        foreach (var template in globalServices)
        {
            NormalizeTemplate(template);
            var key = BuildServiceKey(template.Name, template.Category1, template.Category2);
            if (!tenantServicesByKey.TryGetValue(key, out var tenantService))
                continue;

            var targetOverrides = BuildImportedPackageOverrides(
                tenantService,
                template,
                packageContexts,
                pricingDefaults);

            if (PackageOverridesEquivalent(tenantService.PackageOverrides, targetOverrides))
                continue;

            tenantService.PackageOverrides = targetOverrides;
            db.JobDefinitions.Update(tenantService);
        }
    }

    private static List<JobServicePackageOverride> BuildImportedPackageOverrides(
        JobDefinition tenantService,
        GlobalServiceTemplate globalTemplate,
        IReadOnlyCollection<ImportPackageOverrideContext> packageContexts,
        GlobalDefaultPricingContext pricingDefaults)
    {
        var templateOverridesByGlobalPackageId = new Dictionary<int, JobServicePackageOverride>();
        foreach (var entry in globalTemplate.PackageOverrides ?? new List<JobServicePackageOverride>())
        {
            if (!TryParseGlobalServicePackageTemplateId(entry.ServicePackageJobId, out var globalPackageTemplateId))
                continue;

            templateOverridesByGlobalPackageId[globalPackageTemplateId] = entry;
        }

        var normalizedServiceMinutes = Math.Max(0, tenantService.DefaultMinutes);
        var normalizedServicePrice = ResolveImportedPackagePriceForMinutes(
            tenantService,
            normalizedServiceMinutes,
            pricingDefaults);

        var overrides = new List<JobServicePackageOverride>();
        foreach (var context in packageContexts)
        {
            var reducedMinutes = Math.Max(0, normalizedServiceMinutes - context.TimeReductionMinutes);
            var reducedPrice = ResolveImportedPackagePriceForMinutes(tenantService, reducedMinutes, pricingDefaults);

            if (templateOverridesByGlobalPackageId.TryGetValue(context.GlobalServicePackageTemplateId, out var templateOverride))
            {
                if (!templateOverride.IsAvailableAsAdditionalService)
                {
                    overrides.Add(new JobServicePackageOverride
                    {
                        ServicePackageJobId = context.TenantPackageJobId,
                        IsAvailableAsAdditionalService = false,
                        Minutes = 0,
                        PriceIncVat = 0m
                    });
                    continue;
                }

                var customMinutes = Math.Max(0, templateOverride.Minutes);
                var customPrice = RoundPrice(templateOverride.PriceIncVat);
                if (customMinutes == reducedMinutes && customPrice == reducedPrice)
                    continue;

                overrides.Add(new JobServicePackageOverride
                {
                    ServicePackageJobId = context.TenantPackageJobId,
                    IsAvailableAsAdditionalService = true,
                    Minutes = customMinutes,
                    PriceIncVat = customPrice
                });
                continue;
            }

            if (reducedMinutes == normalizedServiceMinutes && reducedPrice == normalizedServicePrice)
                continue;

            overrides.Add(new JobServicePackageOverride
            {
                ServicePackageJobId = context.TenantPackageJobId,
                IsAvailableAsAdditionalService = true,
                Minutes = reducedMinutes,
                PriceIncVat = reducedPrice
            });
        }

        return overrides
            .OrderBy(overrideItem => overrideItem.ServicePackageJobId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static decimal ResolveImportedPackagePriceForMinutes(
        JobDefinition _,
        int minutes,
        GlobalDefaultPricingContext pricingDefaults)
    {
        if (minutes <= 0)
            return 0m;

        var raw = decimal.Round((minutes / 60m) * pricingDefaults.DefaultHourlyRate, 2, MidpointRounding.AwayFromZero);
        return ApplyPriceRounding(raw, pricingDefaults.RoundingIncrement, pricingDefaults.RoundingMode);
    }

    private static decimal ApplyPriceRounding(decimal value, decimal increment, PriceRoundingMode mode)
    {
        var normalizedIncrement = NormalizeRoundingIncrement(increment);
        if (normalizedIncrement <= 0.01m)
            return decimal.Round(value, 2, MidpointRounding.AwayFromZero);

        var ratio = value / normalizedIncrement;
        var roundedRatio = mode switch
        {
            PriceRoundingMode.Up => decimal.Ceiling(ratio),
            PriceRoundingMode.Nearest => decimal.Round(ratio, 0, MidpointRounding.AwayFromZero),
            _ => decimal.Floor(ratio)
        };

        return decimal.Round(roundedRatio * normalizedIncrement, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal NormalizeRoundingIncrement(decimal increment)
    {
        var supported = new[] { 0.01m, 0.25m, 0.50m, 1.00m };
        if (supported.Contains(increment))
            return increment;

        return 0.50m;
    }

    private static decimal NormalizeHourlyRate(decimal value, decimal fallback)
    {
        var normalized = value > 0 ? value : fallback;
        return decimal.Round(normalized, 2, MidpointRounding.AwayFromZero);
    }

    private static bool TryParseGlobalServicePackageTemplateId(string? value, out int globalServicePackageTemplateId)
    {
        globalServicePackageTemplateId = 0;
        if (!int.TryParse((value ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            return false;

        if (parsed <= 0)
            return false;

        globalServicePackageTemplateId = parsed;
        return true;
    }

    private static string BuildGlobalServicePackageTemplateKey(int globalServicePackageTemplateId)
        => Math.Max(0, globalServicePackageTemplateId).ToString(CultureInfo.InvariantCulture);

    private static Dictionary<string, int> ResolveGlobalDefaultPackageReductionsByName(
        Dictionary<string, int>? storedReductions,
        IEnumerable<GlobalServicePackageTemplate> globalPackages)
    {
        var resolved = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var package in globalPackages)
        {
            var packageName = ServicePackageReductionKeyHelper.NormalizePackageName(package.Name);
            if (string.IsNullOrWhiteSpace(packageName))
                continue;

            var reduction = 0;
            if (storedReductions is not null)
            {
                foreach (var candidate in ServicePackageReductionKeyHelper.BuildGlobalPackageReductionKeyCandidates(packageName))
                {
                    if (!storedReductions.TryGetValue(candidate, out var value))
                        continue;

                    reduction = Math.Max(0, value);
                    break;
                }
            }

            resolved[packageName] = Math.Max(0, reduction);
        }

        return resolved;
    }

    private static List<ServicePackageChecklistItemDefinition> BuildResolvedPackageChecklistItems(
        GlobalServicePackageTemplate package,
        IReadOnlyDictionary<int, GlobalServicePackageTemplate> globalPackagesById,
        IReadOnlyDictionary<int, GlobalServiceTemplate> globalServicesById,
        IReadOnlyDictionary<int, JobDefinition> tenantServiceByTemplateId)
    {
        var resolved = new List<ServicePackageChecklistItemDefinition>();
        var traversalPath = new HashSet<int>();
        AppendResolvedPackageItems(
            package,
            traversalPath,
            resolved,
            globalPackagesById,
            globalServicesById,
            tenantServiceByTemplateId);

        for (var i = 0; i < resolved.Count; i++)
            resolved[i].SortOrder = i;

        return resolved;
    }

    private static void AppendResolvedPackageItems(
        GlobalServicePackageTemplate package,
        HashSet<int> traversalPath,
        List<ServicePackageChecklistItemDefinition> resolved,
        IReadOnlyDictionary<int, GlobalServicePackageTemplate> globalPackagesById,
        IReadOnlyDictionary<int, GlobalServiceTemplate> globalServicesById,
        IReadOnlyDictionary<int, JobDefinition> tenantServiceByTemplateId)
    {
        if (!traversalPath.Add(package.Id))
            return;

        foreach (var rawItem in package.Items
                     .OrderBy(i => i.SortOrder)
                     .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase))
        {
            var item = CloneGlobalPackageItem(rawItem);
            switch (item.ItemType)
            {
                case GlobalServicePackageItemType.IncludePackage:
                    if (!item.IncludedGlobalServicePackageTemplateId.HasValue)
                        continue;

                    if (!globalPackagesById.TryGetValue(item.IncludedGlobalServicePackageTemplateId.Value, out var included))
                        continue;

                    if (traversalPath.Contains(included.Id))
                        continue;

                    AppendResolvedPackageItems(
                        included,
                        traversalPath,
                        resolved,
                        globalPackagesById,
                        globalServicesById,
                        tenantServiceByTemplateId);
                    continue;

                case GlobalServicePackageItemType.Service:
                {
                    string linkedServiceJobId = "";
                    var name = (item.Name ?? "").Trim();
                    var description = (item.Description ?? "").Trim();

                    if (item.LinkedGlobalServiceTemplateId.HasValue
                        && globalServicesById.TryGetValue(item.LinkedGlobalServiceTemplateId.Value, out var globalService))
                    {
                        if (string.IsNullOrWhiteSpace(name))
                            name = (globalService.Name ?? "").Trim();

                        if (string.IsNullOrWhiteSpace(description))
                            description = (globalService.Description ?? "").Trim();

                        if (tenantServiceByTemplateId.TryGetValue(globalService.Id, out var tenantService))
                            linkedServiceJobId = (tenantService.Id ?? "").Trim();
                    }

                    if (string.IsNullOrWhiteSpace(linkedServiceJobId) && string.IsNullOrWhiteSpace(name))
                        continue;

                    resolved.Add(new ServicePackageChecklistItemDefinition
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        SortOrder = resolved.Count,
                        Name = name,
                        Description = description,
                        LinkedServiceJobId = linkedServiceJobId
                    });
                    continue;
                }

                case GlobalServicePackageItemType.Manual:
                default:
                {
                    var name = (item.Name ?? "").Trim();
                    var description = (item.Description ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(description))
                        continue;

                    resolved.Add(new ServicePackageChecklistItemDefinition
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        SortOrder = resolved.Count,
                        Name = name,
                        Description = description,
                        LinkedServiceJobId = ""
                    });
                    continue;
                }
            }
        }

        traversalPath.Remove(package.Id);
    }

    private static bool PackageChecklistEquivalent(
        IReadOnlyCollection<ServicePackageChecklistItemDefinition>? left,
        IReadOnlyCollection<ServicePackageChecklistItemDefinition>? right)
    {
        var orderedLeft = (left ?? Array.Empty<ServicePackageChecklistItemDefinition>())
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var orderedRight = (right ?? Array.Empty<ServicePackageChecklistItemDefinition>())
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (orderedLeft.Count != orderedRight.Count)
            return false;

        for (var i = 0; i < orderedLeft.Count; i++)
        {
            var l = orderedLeft[i];
            var r = orderedRight[i];
            if (!string.Equals((l.Name ?? "").Trim(), (r.Name ?? "").Trim(), StringComparison.Ordinal))
                return false;
            if (!string.Equals((l.Description ?? "").Trim(), (r.Description ?? "").Trim(), StringComparison.Ordinal))
                return false;
            if (!string.Equals((l.LinkedServiceJobId ?? "").Trim(), (r.LinkedServiceJobId ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    private static bool PackageOverridesEquivalent(
        IReadOnlyCollection<JobServicePackageOverride>? left,
        IReadOnlyCollection<JobServicePackageOverride>? right)
    {
        var orderedLeft = NormalizeJobPackageOverridesForComparison(left);
        var orderedRight = NormalizeJobPackageOverridesForComparison(right);

        if (orderedLeft.Count != orderedRight.Count)
            return false;

        for (var i = 0; i < orderedLeft.Count; i++)
        {
            var l = orderedLeft[i];
            var r = orderedRight[i];
            if (!string.Equals(l.ServicePackageJobId, r.ServicePackageJobId, StringComparison.OrdinalIgnoreCase))
                return false;
            if (l.IsAvailableAsAdditionalService != r.IsAvailableAsAdditionalService)
                return false;
            if (l.Minutes != r.Minutes)
                return false;
            if (l.PriceIncVat != r.PriceIncVat)
                return false;
        }

        return true;
    }

    private static List<JobServicePackageOverride> NormalizeJobPackageOverridesForComparison(
        IReadOnlyCollection<JobServicePackageOverride>? overrides)
    {
        return (overrides ?? Array.Empty<JobServicePackageOverride>())
            .Select(entry => new JobServicePackageOverride
            {
                ServicePackageJobId = (entry.ServicePackageJobId ?? "").Trim(),
                IsAvailableAsAdditionalService = entry.IsAvailableAsAdditionalService,
                Minutes = entry.IsAvailableAsAdditionalService ? Math.Max(0, entry.Minutes) : 0,
                PriceIncVat = entry.IsAvailableAsAdditionalService ? RoundPrice(entry.PriceIncVat) : 0m
            })
            .Where(entry => !string.IsNullOrWhiteSpace(entry.ServicePackageJobId))
            .OrderBy(entry => entry.ServicePackageJobId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static async Task<CatalogSettings> LoadOrCreateCatalogSettingsAsync(WorkshopDbContext db, int tenantId)
    {
        var settings = await db.CatalogSettings.FirstOrDefaultAsync(x => x.TenantId == tenantId);
        if (settings is not null)
            return settings;

        settings = SeedData.DefaultCatalogSettings(tenantId);
        db.CatalogSettings.Add(settings);
        await db.SaveChangesAsync();
        return settings;
    }

    private static string BuildServiceKey(JobDefinition service)
        => BuildServiceKey(service.Name, service.Category, service.Category2);

    private static string BuildServiceKey(string name, string category1, string category2)
        => $"{(name ?? "").Trim().ToUpperInvariant()}|{NormalizeCategory1(category1).ToUpperInvariant()}|{NormalizeCategory2(category2).ToUpperInvariant()}";

    private static int ParseJobIndex(string? jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            return 0;

        var lastToken = jobId.Split('_').LastOrDefault();
        return int.TryParse(lastToken, out var parsed) ? parsed : 0;
    }

    private static string NextJobId(int index)
        => $"JOB_{Math.Max(1, index):000}";

    private static string NextServicePackageJobId(string packageName, HashSet<string> usedJobIds)
    {
        var token = SlugifyToken(packageName);
        var baseId = $"SVC_{token}";
        var candidate = baseId;
        var suffix = 2;

        while (usedJobIds.Contains(candidate))
        {
            candidate = $"{baseId}_{suffix}";
            suffix++;
        }

        usedJobIds.Add(candidate);
        return candidate;
    }

    private static string SlugifyToken(string? value)
    {
        var builder = new StringBuilder();
        var lastWasUnderscore = false;
        foreach (var ch in (value ?? "").Trim().ToUpperInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
                lastWasUnderscore = false;
            }
            else if (!lastWasUnderscore)
            {
                builder.Append('_');
                lastWasUnderscore = true;
            }
        }

        var token = builder.ToString().Trim('_');
        return string.IsNullOrWhiteSpace(token) ? "PACKAGE" : token;
    }

    private static void NormalizeTemplate(GlobalServiceTemplate template)
    {
        template.Name = (template.Name ?? "").Trim();
        template.PartNumber = (template.PartNumber ?? "").Trim();
        template.Category1 = NormalizeCategory1(template.Category1);
        template.Category2 = NormalizeCategory2(template.Category2);
        template.SkillLevel = (template.SkillLevel ?? "").Trim();
        template.Description = (template.Description ?? "").Trim();
        template.DefaultMinutes = ResolveTemplateMinutes(template);
        template.BasePriceIncVat = RoundPrice(template.BasePriceIncVat);
        template.EstimatedPriceIncVat = RoundPrice(Math.Max(0m, template.EstimatedPriceIncVat));
        if (!Enum.IsDefined(template.PricingMode))
            template.PricingMode = ServicePricingMode.FixedPrice;
        if (!Enum.IsDefined(template.AutoPricingTier))
            template.AutoPricingTier = ServiceHourlyRateTier.Default;
        template.PackageOverrides = NormalizeGlobalTemplatePackageOverrides(template.PackageOverrides);
    }

    private static List<JobServicePackageOverride> NormalizeGlobalTemplatePackageOverrides(
        IEnumerable<JobServicePackageOverride>? overrides)
    {
        var normalized = new Dictionary<int, JobServicePackageOverride>();
        foreach (var entry in overrides ?? Enumerable.Empty<JobServicePackageOverride>())
        {
            if (!TryParseGlobalServicePackageTemplateId(entry.ServicePackageJobId, out var globalServicePackageTemplateId))
                continue;

            var available = entry.IsAvailableAsAdditionalService;
            normalized[globalServicePackageTemplateId] = new JobServicePackageOverride
            {
                ServicePackageJobId = BuildGlobalServicePackageTemplateKey(globalServicePackageTemplateId),
                IsAvailableAsAdditionalService = available,
                Minutes = available ? Math.Max(0, entry.Minutes) : 0,
                PriceIncVat = available ? RoundPrice(entry.PriceIncVat) : 0m
            };
        }

        return normalized
            .OrderBy(pair => pair.Key)
            .Select(pair => pair.Value)
            .ToList();
    }

    private static void NormalizeGlobalPackageTemplate(GlobalServicePackageTemplate template)
    {
        template.Name = (template.Name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(template.Name))
            template.Name = "Service package";

        template.SkillLevel = (template.SkillLevel ?? "").Trim();
        if (string.IsNullOrWhiteSpace(template.SkillLevel))
            template.SkillLevel = "All";

        template.Description = (template.Description ?? "").Trim();
        template.DefaultMinutes = ResolveGlobalPackageMinutes(template);
        template.BasePriceIncVat = RoundPrice(template.BasePriceIncVat);
        template.EstimatedPriceIncVat = RoundPrice(Math.Max(0m, template.EstimatedPriceIncVat));
        template.SortOrder = Math.Max(0, template.SortOrder);

        if (!Enum.IsDefined(template.PricingMode))
            template.PricingMode = ServicePricingMode.FixedPrice;
        if (!Enum.IsDefined(template.AutoPricingTier))
            template.AutoPricingTier = ServiceHourlyRateTier.Default;

        template.Items = NormalizeGlobalPackageItems(template.Items);
    }

    private static int ResolveGlobalPackageMinutes(GlobalServicePackageTemplate template)
    {
        if (template.PricingMode == ServicePricingMode.EstimatedPrice
            && template.BasePriceIncVat <= 0m
            && template.EstimatedPriceIncVat <= 0m)
        {
            return Math.Max(0, template.DefaultMinutes);
        }

        return Math.Max(1, template.DefaultMinutes);
    }

    private static List<GlobalServicePackageItemDefinition> NormalizeGlobalPackageItems(
        IEnumerable<GlobalServicePackageItemDefinition>? items)
    {
        var normalized = new List<GlobalServicePackageItemDefinition>();
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in items ?? Enumerable.Empty<GlobalServicePackageItemDefinition>())
        {
            var item = CloneGlobalPackageItem(raw);
            if (!Enum.IsDefined(item.ItemType))
                item.ItemType = GlobalServicePackageItemType.Manual;

            item.SortOrder = Math.Max(0, item.SortOrder);
            item.Name = (item.Name ?? "").Trim();
            item.Description = (item.Description ?? "").Trim();

            if (item.ItemType == GlobalServicePackageItemType.Manual
                && string.IsNullOrWhiteSpace(item.Name)
                && string.IsNullOrWhiteSpace(item.Description))
            {
                continue;
            }

            if (item.ItemType == GlobalServicePackageItemType.Service
                && !item.LinkedGlobalServiceTemplateId.HasValue
                && string.IsNullOrWhiteSpace(item.Name))
            {
                continue;
            }

            if (item.ItemType == GlobalServicePackageItemType.IncludePackage
                && !item.IncludedGlobalServicePackageTemplateId.HasValue)
            {
                continue;
            }

            var id = (item.Id ?? "").Trim();
            if (string.IsNullOrWhiteSpace(id) || !seenIds.Add(id))
                item.Id = Guid.NewGuid().ToString("N");
            else
                item.Id = id;

            normalized.Add(item);
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

    private static GlobalServicePackageItemDefinition CloneGlobalPackageItem(GlobalServicePackageItemDefinition item)
        => new()
        {
            Id = (item.Id ?? "").Trim(),
            SortOrder = Math.Max(0, item.SortOrder),
            ItemType = item.ItemType,
            Name = (item.Name ?? "").Trim(),
            Description = (item.Description ?? "").Trim(),
            LinkedGlobalServiceTemplateId = item.LinkedGlobalServiceTemplateId,
            IncludedGlobalServicePackageTemplateId = item.IncludedGlobalServicePackageTemplateId
        };

    private static int ResolveTemplateMinutes(GlobalServiceTemplate template)
    {
        if (RequiresManualInput(template))
            return Math.Max(0, template.DefaultMinutes);

        return Math.Max(1, template.DefaultMinutes);
    }

    private static bool RequiresManualInput(GlobalServiceTemplate template)
        => template.PricingMode == ServicePricingMode.EstimatedPrice
           && template.BasePriceIncVat <= 0m
           && template.EstimatedPriceIncVat <= 0m;

    private static string NormalizeCategory1(string? value)
    {
        var normalized = (value ?? "").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "Uncategorized" : normalized;
    }

    private static string NormalizeCategory2(string? value)
        => (value ?? "").Trim();

    private static string NormalizeColor(string? value)
    {
        var normalized = (value ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return "#94a3b8";

        if (!normalized.StartsWith("#", StringComparison.Ordinal))
            normalized = "#" + normalized;

        return normalized;
    }

    private static decimal RoundPrice(decimal value)
        => decimal.Round(Math.Max(0m, value), 2, MidpointRounding.AwayFromZero);

    private sealed record ImportPackageOverrideContext(
        int GlobalServicePackageTemplateId,
        string TenantPackageJobId,
        int TimeReductionMinutes);

    private sealed record GlobalDefaultPricingContext(
        decimal DefaultHourlyRate,
        decimal DiscountedHourlyRate,
        decimal LossLeaderHourlyRate,
        decimal RoundingIncrement,
        PriceRoundingMode RoundingMode);
}
