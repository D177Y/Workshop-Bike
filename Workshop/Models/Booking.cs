namespace Workshop.Models;

public sealed class Booking
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int StoreId { get; set; }
    public int MechanicId { get; set; }

    public string Title { get; set; } = "";
    public DateTime Start { get; set; }
    public DateTime End { get; set; }

    public string JobId { get; set; } = "";
    public string[] JobIds { get; set; } = Array.Empty<string>();
    public string[] AddOnIds { get; set; } = Array.Empty<string>();

    public int TotalMinutes { get; set; }
    public decimal TotalPriceIncVat { get; set; }
    public string StatusName { get; set; } = "Scheduled";

    public string CustomerFirstName { get; set; } = "";
    public string CustomerLastName { get; set; } = "";
    public string CustomerAccountNumber { get; set; } = "";
    public string CustomerPhone { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public string BikeDetails { get; set; } = "";
    public string Notes { get; set; } = "";
    public string SourceQuoteId { get; set; } = "";
    public BookingJobCard? JobCard { get; set; }
}

