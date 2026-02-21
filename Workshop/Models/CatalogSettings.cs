namespace Workshop.Models;

public sealed class CatalogSettings
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public Dictionary<string, string> CategoryColors { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, List<string>> ServiceCategoryHierarchy { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> SkillLevels { get; set; } = new();
    public bool AutomaticServicePricingEnabled { get; set; }
    public decimal DefaultHourlyRate { get; set; } = 76m;
    public decimal DiscountedHourlyRate { get; set; } = 60m;
    public decimal LossLeaderHourlyRate { get; set; } = 50m;
    public decimal AutoPriceRoundingIncrement { get; set; } = 0.50m;
    public PriceRoundingMode AutoPriceRoundingMode { get; set; } = PriceRoundingMode.Down;
    public Dictionary<string, int> ServicePackageAddOnTimeReductions { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
