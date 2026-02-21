namespace Workshop.Models;

public sealed class GlobalServiceTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string PartNumber { get; set; } = "";
    public string Category1 { get; set; } = "";
    public string Category2 { get; set; } = "";
    public string SkillLevel { get; set; } = "All";
    public string Description { get; set; } = "";
    public int DefaultMinutes { get; set; } = 30;
    public decimal BasePriceIncVat { get; set; }
    public ServicePricingMode PricingMode { get; set; } = ServicePricingMode.FixedPrice;
    public ServiceHourlyRateTier AutoPricingTier { get; set; } = ServiceHourlyRateTier.Default;
    public decimal EstimatedPriceIncVat { get; set; }
    public int SortOrder { get; set; }
}
