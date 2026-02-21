using Microsoft.EntityFrameworkCore;
using Workshop.Data;
using Workshop.Models;

namespace Workshop.Services;

public sealed class SuggestedCatalogService
{
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

        var doCategories = mode is SuggestedImportMode.CategoriesOnly or SuggestedImportMode.ServicesOnly or SuggestedImportMode.ServicesAndTimes or SuggestedImportMode.FullSetup;
        var doServices = mode is SuggestedImportMode.ServicesOnly or SuggestedImportMode.ServicesAndTimes or SuggestedImportMode.ServicePrices or SuggestedImportMode.FullSetup;
        var includeTimes = mode is SuggestedImportMode.ServicesAndTimes or SuggestedImportMode.FullSetup;
        var includePrices = mode is SuggestedImportMode.ServicePrices or SuggestedImportMode.FullSetup;

        if (doCategories)
            ApplyCategories(settings, globalCategories, result);

        if (doServices)
            await ApplyServicesAsync(db, tenantId, globalServices, includeTimes, includePrices, result);

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
            .Where(j => j.TenantId == tenantId && !j.Category.Equals("Service Packages", StringComparison.OrdinalIgnoreCase))
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
                    DefaultMinutes = includeTimes ? Math.Max(1, template.DefaultMinutes) : 30,
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
                var targetMinutes = Math.Max(1, template.DefaultMinutes);
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

    private static void NormalizeTemplate(GlobalServiceTemplate template)
    {
        template.Name = (template.Name ?? "").Trim();
        template.PartNumber = (template.PartNumber ?? "").Trim();
        template.Category1 = NormalizeCategory1(template.Category1);
        template.Category2 = NormalizeCategory2(template.Category2);
        template.SkillLevel = (template.SkillLevel ?? "").Trim();
        template.Description = (template.Description ?? "").Trim();
        template.DefaultMinutes = Math.Max(1, template.DefaultMinutes);
        template.BasePriceIncVat = RoundPrice(template.BasePriceIncVat);
        template.EstimatedPriceIncVat = RoundPrice(Math.Max(0m, template.EstimatedPriceIncVat));
        if (!Enum.IsDefined(template.PricingMode))
            template.PricingMode = ServicePricingMode.FixedPrice;
        if (!Enum.IsDefined(template.AutoPricingTier))
            template.AutoPricingTier = ServiceHourlyRateTier.Default;
    }

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
}
