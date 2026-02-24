using Workshop.Models;

namespace Workshop.Services;

public static class StripeBillingPolicy
{
    public static string NormalizeStatus(string? rawStatus)
        => (rawStatus ?? "").Trim().ToLowerInvariant();

    public static bool IsPaidStatus(string? rawStatus)
    {
        var status = NormalizeStatus(rawStatus);
        return status is "active" or "trialing" or "past_due";
    }

    public static bool IsTerminalUnpaidStatus(string? rawStatus)
    {
        var status = NormalizeStatus(rawStatus);
        return status is "canceled" or "cancelled" or "unpaid" or "incomplete_expired" or "paused";
    }

    public static bool HasBillableAccess(Tenant tenant)
    {
        if (tenant is null)
            return false;

        if (IsPaidStatus(tenant.StripeSubscriptionStatus))
            return true;

        // Legacy fallback: older rows may have a subscription id but no synced status yet.
        return !string.IsNullOrWhiteSpace(tenant.StripeSubscriptionId)
               && string.IsNullOrWhiteSpace(NormalizeStatus(tenant.StripeSubscriptionStatus));
    }
}
