namespace Workshop.Models;

public sealed class ServicePivotRecord
{
    public string RowLabel { get; set; } = "";
    public string ServicePackage { get; set; } = "";
    public int Units { get; set; }
}
