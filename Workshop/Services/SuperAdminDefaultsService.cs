using Microsoft.EntityFrameworkCore;
using Workshop.Data;
using Workshop.Models;

namespace Workshop.Services;

public sealed class SuperAdminDefaultsService
{
    private const int GlobalDefaultsTenantId = 0;
    private readonly IDbContextFactory<WorkshopDbContext> _dbFactory;

    public SuperAdminDefaultsService(IDbContextFactory<WorkshopDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<SuperAdminDefaultRates> GetHourlyRatesAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var settings = await LoadOrCreateGlobalCatalogSettingsAsync(db);
        return new SuperAdminDefaultRates(
            decimal.Round(settings.DefaultHourlyRate, 2, MidpointRounding.AwayFromZero),
            decimal.Round(settings.DiscountedHourlyRate, 2, MidpointRounding.AwayFromZero),
            decimal.Round(settings.LossLeaderHourlyRate, 2, MidpointRounding.AwayFromZero),
            NormalizeRoundingIncrement(settings.AutoPriceRoundingIncrement));
    }

    public async Task<SuperAdminDefaultRatesSaveResult> SaveHourlyRatesAsync(
        SuperAdminDefaultRates rates,
        bool recalculateAutoRateTemplates)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var settings = await LoadOrCreateGlobalCatalogSettingsAsync(db);

        settings.DefaultHourlyRate = NormalizeHourlyRate(rates.DefaultHourlyRate, 76m);
        settings.DiscountedHourlyRate = NormalizeHourlyRate(rates.DiscountedHourlyRate, 60m);
        settings.LossLeaderHourlyRate = NormalizeHourlyRate(rates.LossLeaderHourlyRate, 50m);
        settings.AutoPriceRoundingMode = PriceRoundingMode.Down;
        settings.AutoPriceRoundingIncrement = NormalizeRoundingIncrement(rates.RoundingIncrement);

        var recalculated = 0;
        if (recalculateAutoRateTemplates)
        {
            var templates = await db.GlobalServiceTemplates.ToListAsync();
            foreach (var template in templates)
            {
                if (template.PricingMode != ServicePricingMode.AutoRate)
                    continue;

                var rate = template.AutoPricingTier switch
                {
                    ServiceHourlyRateTier.Discounted => settings.DiscountedHourlyRate,
                    ServiceHourlyRateTier.LossLeader => settings.LossLeaderHourlyRate,
                    _ => settings.DefaultHourlyRate
                };

                var autoPrice = decimal.Round((Math.Max(0, template.DefaultMinutes) / 60m) * rate, 2, MidpointRounding.AwayFromZero);
                var price = ApplyRoundingDown(autoPrice, settings.AutoPriceRoundingIncrement);
                if (template.BasePriceIncVat == price)
                    continue;

                template.BasePriceIncVat = price;
                recalculated++;
            }
        }

        await db.SaveChangesAsync();

        return new SuperAdminDefaultRatesSaveResult(
            settings.DefaultHourlyRate,
            settings.DiscountedHourlyRate,
            settings.LossLeaderHourlyRate,
            settings.AutoPriceRoundingIncrement,
            recalculated);
    }

    public async Task<SuperAdminDefaultAddOnRates> GetDefaultAddOnRatesAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var settings = await LoadOrCreateGlobalCatalogSettingsAsync(db);
        var packages = await db.GlobalServicePackageTemplates
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .ToListAsync();

        return BuildDefaultAddOnRates(settings, packages);
    }

    public async Task<SuperAdminDefaultAddOnRates> SaveDefaultAddOnRatesAsync(
        Dictionary<int, int> reductionsByGlobalPackageId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var settings = await LoadOrCreateGlobalCatalogSettingsAsync(db);
        var packages = await db.GlobalServicePackageTemplates
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .ToListAsync();

        var reductionsByPackageName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var package in packages)
        {
            var packageName = ServicePackageReductionKeyHelper.NormalizePackageName(package.Name);
            if (string.IsNullOrWhiteSpace(packageName))
                continue;

            var reduction = reductionsByGlobalPackageId.TryGetValue(package.Id, out var value)
                ? Math.Max(0, value)
                : 0;
            reductionsByPackageName[packageName] = reduction;
        }

        settings.ServicePackageAddOnTimeReductions = reductionsByPackageName
            .ToDictionary(
                pair => ServicePackageReductionKeyHelper.BuildGlobalPackageReductionKey(pair.Key),
                pair => pair.Value,
                StringComparer.OrdinalIgnoreCase);

        await db.SaveChangesAsync();
        return BuildDefaultAddOnRates(settings, packages);
    }

    private static SuperAdminDefaultAddOnRates BuildDefaultAddOnRates(
        CatalogSettings settings,
        IReadOnlyCollection<GlobalServicePackageTemplate> packages)
    {
        var reductionsByPackageName = ResolveGlobalPackageReductionsByPackageName(
            settings.ServicePackageAddOnTimeReductions,
            packages);

        var rows = packages
            .Select(package =>
            {
                var packageName = ServicePackageReductionKeyHelper.NormalizePackageName(package.Name);
                var reduction = reductionsByPackageName.TryGetValue(packageName, out var value)
                    ? value
                    : 0;

                return new SuperAdminDefaultAddOnRatePackage(
                    package.Id,
                    packageName,
                    Math.Max(0, reduction));
            })
            .ToList();

        return new SuperAdminDefaultAddOnRates(
            decimal.Round(settings.DefaultHourlyRate, 2, MidpointRounding.AwayFromZero),
            decimal.Round(settings.DiscountedHourlyRate, 2, MidpointRounding.AwayFromZero),
            decimal.Round(settings.LossLeaderHourlyRate, 2, MidpointRounding.AwayFromZero),
            rows);
    }

    private static Dictionary<string, int> ResolveGlobalPackageReductionsByPackageName(
        Dictionary<string, int>? storedReductions,
        IEnumerable<GlobalServicePackageTemplate> packages)
    {
        var resolved = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var package in packages)
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

    private static decimal ApplyRoundingDown(decimal value, decimal increment)
    {
        var normalizedIncrement = NormalizeRoundingIncrement(increment);
        if (normalizedIncrement <= 0.01m)
            return decimal.Round(value, 2, MidpointRounding.AwayFromZero);

        var ratio = value / normalizedIncrement;
        var roundedRatio = decimal.Floor(ratio);
        return decimal.Round(roundedRatio * normalizedIncrement, 2, MidpointRounding.AwayFromZero);
    }

    private static async Task<CatalogSettings> LoadOrCreateGlobalCatalogSettingsAsync(WorkshopDbContext db)
    {
        var settings = await db.CatalogSettings.FirstOrDefaultAsync(x => x.TenantId == GlobalDefaultsTenantId);
        if (settings is not null)
            return settings;

        settings = SeedData.DefaultCatalogSettings(GlobalDefaultsTenantId);
        db.CatalogSettings.Add(settings);
        await db.SaveChangesAsync();
        return settings;
    }
}

public sealed record SuperAdminDefaultRates(
    decimal DefaultHourlyRate,
    decimal DiscountedHourlyRate,
    decimal LossLeaderHourlyRate,
    decimal RoundingIncrement);

public sealed record SuperAdminDefaultRatesSaveResult(
    decimal DefaultHourlyRate,
    decimal DiscountedHourlyRate,
    decimal LossLeaderHourlyRate,
    decimal RoundingIncrement,
    int RecalculatedTemplateCount);

public sealed record SuperAdminDefaultAddOnRates(
    decimal DefaultHourlyRate,
    decimal DiscountedHourlyRate,
    decimal LossLeaderHourlyRate,
    IReadOnlyList<SuperAdminDefaultAddOnRatePackage> Packages);

public sealed record SuperAdminDefaultAddOnRatePackage(
    int GlobalServicePackageTemplateId,
    string PackageName,
    int TimeReductionMinutes);
