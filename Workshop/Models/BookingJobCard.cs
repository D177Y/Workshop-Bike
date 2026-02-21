namespace Workshop.Models;

public sealed class BookingJobCard
{
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
    public int AssignedMechanicId { get; set; }
    public string StatusName { get; set; } = "Scheduled";

    public List<JobCardServiceItem> Services { get; set; } = new();
    public List<JobCardPartItem> Parts { get; set; } = new();
    public List<JobCardMessageLogItem> MessageLog { get; set; } = new();

    public string ServiceNotes { get; set; } = "";
    public string CustomerNotes { get; set; } = "";
    public string CommunicationDraft { get; set; } = "";
    public DateTime? LastSmsSentUtc { get; set; }
    public DateTime? LastEmailSentUtc { get; set; }
}

public sealed class JobCardServiceItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string JobId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int EstimatedMinutes { get; set; }
    public decimal EstimatedPriceIncVat { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsPackageChecklistItem { get; set; }
    public string ParentPackageServiceItemId { get; set; } = "";
    public string ChecklistTemplateItemId { get; set; } = "";
    public string ChecklistSourceJobId { get; set; } = "";
    public int ChecklistSortOrder { get; set; }
}

public sealed class JobCardPartItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal UnitPriceIncVat { get; set; }
    public int Quantity { get; set; } = 1;
    public bool IsFitted { get; set; }
}

public sealed class JobCardMessageLogItem
{
    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
    public string Channel { get; set; } = "";
    public string Recipient { get; set; } = "";
    public string Summary { get; set; } = "";
}
