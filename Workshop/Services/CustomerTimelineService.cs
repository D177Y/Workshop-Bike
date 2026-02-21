using Workshop.Models;

namespace Workshop.Services;

public sealed class CustomerTimelineEntry
{
    public DateTime OccurredUtc { get; init; }
    public string OccurredLocal { get; init; } = "";
    public string Type { get; init; } = "";
    public string Channel { get; init; } = "";
    public string Target { get; init; } = "";
    public string Summary { get; init; } = "";
    public string Source { get; init; } = "";
}

public static class CustomerTimelineService
{
    public static List<CustomerTimelineEntry> BuildTimeline(CustomerProfile customer, IEnumerable<Booking> bookings, DateTime utcNow)
    {
        var timeline = new List<CustomerTimelineEntry>();

        foreach (var quote in customer.Quotes)
        {
            var status = QuoteLifecycleService.ResolveStatus(quote, utcNow);

            timeline.Add(new CustomerTimelineEntry
            {
                OccurredUtc = quote.CreatedUtc,
                OccurredLocal = quote.CreatedUtc.ToLocalTime().ToString("dd MMM yyyy HH:mm"),
                Type = "Quote",
                Channel = "System",
                Target = customer.AccountNumber,
                Summary = $"Quote created ({quote.Id}) - {status} - GBP {quote.EstimatedPriceIncVat:0.00}",
                Source = string.IsNullOrWhiteSpace(quote.StoreName) ? "Assessment" : quote.StoreName
            });

            if (quote.SentAtUtc.HasValue)
            {
                var sentAt = quote.SentAtUtc.Value;
                timeline.Add(new CustomerTimelineEntry
                {
                    OccurredUtc = sentAt,
                    OccurredLocal = sentAt.ToLocalTime().ToString("dd MMM yyyy HH:mm"),
                    Type = "Quote",
                    Channel = "Email",
                    Target = string.IsNullOrWhiteSpace(customer.Email) ? customer.AccountNumber : customer.Email,
                    Summary = $"Quote sent ({quote.Id})",
                    Source = "Assessment quote"
                });
            }

            if (quote.AcceptedAtUtc.HasValue)
            {
                var acceptedAt = quote.AcceptedAtUtc.Value;
                timeline.Add(new CustomerTimelineEntry
                {
                    OccurredUtc = acceptedAt,
                    OccurredLocal = acceptedAt.ToLocalTime().ToString("dd MMM yyyy HH:mm"),
                    Type = "Quote",
                    Channel = "System",
                    Target = customer.AccountNumber,
                    Summary = $"Quote accepted ({quote.Id})",
                    Source = "Booking"
                });
            }
        }

        foreach (var communication in customer.Communications)
        {
            timeline.Add(new CustomerTimelineEntry
            {
                OccurredUtc = communication.SentAtUtc,
                OccurredLocal = communication.SentAtUtc.ToLocalTime().ToString("dd MMM yyyy HH:mm"),
                Type = "Communication",
                Channel = string.IsNullOrWhiteSpace(communication.Channel) ? "-" : communication.Channel,
                Target = string.IsNullOrWhiteSpace(communication.Recipient) ? "-" : communication.Recipient,
                Summary = string.IsNullOrWhiteSpace(communication.Summary) ? "-" : communication.Summary,
                Source = string.IsNullOrWhiteSpace(communication.Source) ? "Customer profile" : communication.Source
            });
        }

        foreach (var booking in bookings)
        {
            timeline.Add(new CustomerTimelineEntry
            {
                OccurredUtc = booking.Start,
                OccurredLocal = booking.Start.ToLocalTime().ToString("dd MMM yyyy HH:mm"),
                Type = "Booking",
                Channel = "System",
                Target = booking.StatusName,
                Summary = $"Booking #{booking.Id}: {booking.Title} (GBP {booking.TotalPriceIncVat:0.00})",
                Source = "Bookings"
            });

            if (booking.JobCard?.MessageLog is not { Count: > 0 })
                continue;

            foreach (var message in booking.JobCard.MessageLog)
            {
                timeline.Add(new CustomerTimelineEntry
                {
                    OccurredUtc = message.SentAtUtc,
                    OccurredLocal = message.SentAtUtc.ToLocalTime().ToString("dd MMM yyyy HH:mm"),
                    Type = "Communication",
                    Channel = string.IsNullOrWhiteSpace(message.Channel) ? "-" : message.Channel,
                    Target = string.IsNullOrWhiteSpace(message.Recipient) ? "-" : message.Recipient,
                    Summary = string.IsNullOrWhiteSpace(message.Summary) ? "-" : message.Summary,
                    Source = $"Booking #{booking.Id}"
                });
            }
        }

        return timeline
            .OrderByDescending(x => x.OccurredUtc)
            .ToList();
    }
}
