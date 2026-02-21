namespace Workshop.Models;

public sealed class TimetasticWebhookEvent
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public long EventId { get; set; }
    public string EventType { get; set; } = "";
    public DateTime ReceivedUtc { get; set; }
    public DateTime ProcessedUtc { get; set; }
    public string Outcome { get; set; } = "";
}
