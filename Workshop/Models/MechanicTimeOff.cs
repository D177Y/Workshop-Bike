namespace Workshop.Models;

public sealed class MechanicTimeOff
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int StoreId { get; set; }
    public int MechanicId { get; set; }
    public string Type { get; set; } = "Holiday";
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public bool IsAllDay { get; set; }
    public string Notes { get; set; } = "";
    public string Source { get; set; } = "Manual";
    public string ExternalId { get; set; } = "";
    public DateTime? LastSyncedUtc { get; set; }
}
