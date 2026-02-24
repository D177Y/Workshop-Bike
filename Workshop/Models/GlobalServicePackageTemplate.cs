namespace Workshop.Models;

public sealed class GlobalServicePackageTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string SkillLevel { get; set; } = "All";
    public string Description { get; set; } = "";
    public int DefaultMinutes { get; set; } = 60;
    public decimal BasePriceIncVat { get; set; }
    public ServicePricingMode PricingMode { get; set; } = ServicePricingMode.FixedPrice;
    public ServiceHourlyRateTier AutoPricingTier { get; set; } = ServiceHourlyRateTier.Default;
    public decimal EstimatedPriceIncVat { get; set; }
    public int SortOrder { get; set; }
    public List<GlobalServicePackageItemDefinition> Items { get; set; } = new();
}
