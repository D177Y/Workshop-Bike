namespace Workshop.Models;

public sealed class Booking
{
    public int Id { get; set; }
    public int StoreId { get; set; }
    public int MechanicId { get; set; }

    public string Title { get; set; } = "";
    public DateTime Start { get; set; }
    public DateTime End { get; set; }

    public string JobId { get; set; } = "";
    public string[] AddOnIds { get; set; } = Array.Empty<string>();

    public int TotalMinutes { get; set; }
    public decimal TotalPriceIncVat { get; set; }
}

