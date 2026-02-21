using Workshop.Models;

namespace Workshop.Services;

public static class QuoteLifecycleService
{
    public static string ResolveStatus(CustomerQuoteRecord quote, DateTime utcNow)
    {
        var normalized = QuoteLifecycleStatus.Normalize(quote.Status);
        if (normalized.Equals(QuoteLifecycleStatus.Accepted, StringComparison.OrdinalIgnoreCase))
            return QuoteLifecycleStatus.Accepted;

        if (quote.AcceptedAtUtc.HasValue)
            return QuoteLifecycleStatus.Accepted;

        if (quote.ExpiresAtUtc.HasValue && quote.ExpiresAtUtc.Value <= utcNow)
            return QuoteLifecycleStatus.Expired;

        if (normalized.Equals(QuoteLifecycleStatus.Sent, StringComparison.OrdinalIgnoreCase))
            return QuoteLifecycleStatus.Sent;

        if (quote.SentAtUtc.HasValue)
            return QuoteLifecycleStatus.Sent;

        return QuoteLifecycleStatus.Draft;
    }

    public static void ApplyStatus(CustomerQuoteRecord quote, string status, DateTime utcNow, string? detail = null)
    {
        var normalized = QuoteLifecycleStatus.Normalize(status);
        if (normalized.Equals(QuoteLifecycleStatus.Expired, StringComparison.OrdinalIgnoreCase))
            normalized = QuoteLifecycleStatus.Expired;

        quote.Status = normalized;
        quote.StatusUpdatedUtc = utcNow;

        if (!string.IsNullOrWhiteSpace(detail))
            quote.StatusDetail = detail.Trim();

        if (normalized.Equals(QuoteLifecycleStatus.Sent, StringComparison.OrdinalIgnoreCase) && !quote.SentAtUtc.HasValue)
            quote.SentAtUtc = utcNow;

        if (normalized.Equals(QuoteLifecycleStatus.Accepted, StringComparison.OrdinalIgnoreCase) && !quote.AcceptedAtUtc.HasValue)
            quote.AcceptedAtUtc = utcNow;

        if (normalized.Equals(QuoteLifecycleStatus.Expired, StringComparison.OrdinalIgnoreCase) && !quote.ExpiresAtUtc.HasValue)
            quote.ExpiresAtUtc = utcNow;
    }
}
