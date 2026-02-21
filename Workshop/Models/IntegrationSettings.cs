namespace Workshop.Models;

public sealed class IntegrationSettings
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public bool TimetasticEnabled { get; set; }
    public string TimetasticApiBaseUrl { get; set; } = "https://app.timetastic.co.uk/api/";
    public string TimetasticApiToken { get; set; } = "";
    public string TimetasticWebhookSecret { get; set; } = "";
    public string TimetasticWebhookCallbackUrl { get; set; } = "";
    public DateTime? TimetasticLastSyncUtc { get; set; }
    public DateTime? TimetasticLastWebhookReceivedUtc { get; set; }
    public List<TimetasticMechanicMapping> TimetasticMechanicMappings { get; set; } = new();
}

public sealed class TimetasticMechanicMapping
{
    public int MechanicId { get; set; }
    public string TimetasticUserId { get; set; } = "";
    public string TimetasticUserName { get; set; } = "";
}
