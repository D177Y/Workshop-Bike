namespace Workshop.Models;

public sealed class JobDefinition
{
    public string Id { get; set; } = "";
    public int TenantId { get; set; }
    public string Name { get; set; } = "";
    public string PartNumber { get; set; } = "";
    public string Category { get; set; } = "";
    public string Category2 { get; set; } = "";
    public string SkillLevel { get; set; } = "All";
    public string Description { get; set; } = "";
    public string ColorHex { get; set; } = "";
    public int DefaultMinutes { get; set; }
    public decimal BasePriceIncVat { get; set; }
    public ServicePricingMode PricingMode { get; set; } = ServicePricingMode.FixedPrice;
    public ServiceHourlyRateTier AutoPricingTier { get; set; } = ServiceHourlyRateTier.Default;
    public decimal EstimatedPriceIncVat { get; set; }
    public List<JobServicePackageOverride> PackageOverrides { get; set; } = new();
    public List<ServicePackageChecklistItemDefinition> PackageChecklistItems { get; set; } = new();
}
