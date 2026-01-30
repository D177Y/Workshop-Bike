namespace Workshop.Models;

public sealed class AddOnRule
{
    public string JobId { get; set; } = "";
    public string AddOnId { get; set; } = "";

    // Deltas for this add-on when applied to this job
    public int ExtraMinutes { get; set; }
    public decimal ExtraPriceIncVat { get; set; }
}
