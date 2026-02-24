namespace Workshop.Models;

public sealed class Tenant
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string ContactName { get; set; } = "";
    public string ContactEmail { get; set; } = "";
    public string ContactPhone { get; set; } = "";
    public string AddressLine1 { get; set; } = "";
    public string AddressLine2 { get; set; } = "";
    public string City { get; set; } = "";
    public string Postcode { get; set; } = "";
    public string Country { get; set; } = "United Kingdom";
    public PlanTier Plan { get; set; } = PlanTier.Standard;
    public int MaxMechanics { get; set; } = 6;
    public string StripeCustomerId { get; set; } = "";
    public string StripeSubscriptionId { get; set; } = "";
    public string StripeSubscriptionStatus { get; set; } = "";
    public DateTime? StripeCurrentPeriodEndUtc { get; set; }
    public DateTime? StripeSubscriptionUpdatedAtUtc { get; set; }
    public bool HasActivatedSubscription { get; set; }
    public int FinancialYearStartMonth { get; set; } = 1;
    public int FinancialYearStartDay { get; set; } = 1;
    public int FinancialYearEndMonth { get; set; } = 12;
    public int FinancialYearEndDay { get; set; } = 31;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? TrialDataPurgedAtUtc { get; set; }
}
