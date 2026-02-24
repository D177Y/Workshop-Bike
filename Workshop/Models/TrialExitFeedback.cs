namespace Workshop.Models;

public sealed class TrialExitFeedback
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int? UserId { get; set; }
    public string Disliked { get; set; } = "";
    public string Improvements { get; set; } = "";
    public string NoSignupReason { get; set; } = "";
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
}
