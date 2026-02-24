namespace Workshop.Models;

public sealed class CustomerProfile
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string AccountNumber { get; set; } = "";
    public string PhoneNormalized { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string County { get; set; } = "";
    public string Postcode { get; set; } = "";
    public string AddressLine1 { get; set; } = "";
    public string AddressLine2 { get; set; } = "";

    public List<CustomerBikeProfile> Bikes { get; set; } = new();
    public List<CustomerQuoteRecord> Quotes { get; set; } = new();
    public List<CustomerCommunicationRecord> Communications { get; set; } = new();

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class CustomerBikeProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Make { get; set; } = "";
    public string Model { get; set; } = "";
    public string Size { get; set; } = "";
    public string FrameNumber { get; set; } = "";
    public string StockNumber { get; set; } = "";
}

public sealed class CustomerQuoteRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = QuoteLifecycleStatus.Draft;
    public DateTime StatusUpdatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SentAtUtc { get; set; }
    public DateTime? AcceptedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public string StatusDetail { get; set; } = "";
    public int StoreId { get; set; }
    public string StoreName { get; set; } = "";
    public string BikeDetails { get; set; } = "";
    public string[] JobIds { get; set; } = Array.Empty<string>();
    public string[] JobNames { get; set; } = Array.Empty<string>();
    public List<CustomerQuoteManualServiceOverride> ManualServiceOverrides { get; set; } = new();
    public int EstimatedMinutes { get; set; }
    public decimal EstimatedPriceIncVat { get; set; }
    public string Notes { get; set; } = "";
    public string CreatedBy { get; set; } = "";
}

public sealed class CustomerQuoteManualServiceOverride
{
    public string JobId { get; set; } = "";
    public int Minutes { get; set; }
    public decimal PriceIncVat { get; set; }
}

public sealed class CustomerCommunicationRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
    public string Channel { get; set; } = "";
    public string Direction { get; set; } = "";
    public string Recipient { get; set; } = "";
    public string Summary { get; set; } = "";
    public string DeliveryStatus { get; set; } = "";
    public string DeliveryError { get; set; } = "";
    public bool IsAutomated { get; set; }
    public string Source { get; set; } = "";
}

public static class QuoteLifecycleStatus
{
    public const string Draft = "Draft";
    public const string Sent = "Sent";
    public const string Accepted = "Accepted";
    public const string Expired = "Expired";

    public static string Normalize(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return Draft;

        var value = status.Trim();
        if (value.Equals(Sent, StringComparison.OrdinalIgnoreCase))
            return Sent;
        if (value.Equals(Accepted, StringComparison.OrdinalIgnoreCase))
            return Accepted;
        if (value.Equals(Expired, StringComparison.OrdinalIgnoreCase))
            return Expired;

        return Draft;
    }
}
