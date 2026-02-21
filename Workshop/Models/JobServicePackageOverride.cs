namespace Workshop.Models;

public sealed class JobServicePackageOverride
{
    public string ServicePackageJobId { get; set; } = "";
    public bool IsAvailableAsAdditionalService { get; set; } = true;
    public int Minutes { get; set; }
    public decimal PriceIncVat { get; set; }
}
