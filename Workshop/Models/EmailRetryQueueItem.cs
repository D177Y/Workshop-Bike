namespace Workshop.Models;

public sealed class EmailRetryQueueItem
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string AccountNumber { get; set; } = "";
    public string Recipient { get; set; } = "";
    public string Subject { get; set; } = "";
    public string HtmlBody { get; set; } = "";
    public string TextBody { get; set; } = "";
    public string Source { get; set; } = "";
    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; } = 5;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastAttemptUtc { get; set; }
    public DateTime NextAttemptUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SentUtc { get; set; }
    public string Status { get; set; } = EmailRetryStatus.Pending;
    public string LastError { get; set; } = "";
}

public static class EmailRetryStatus
{
    public const string Pending = "Pending";
    public const string Sent = "Sent";
    public const string DeadLetter = "DeadLetter";
}
