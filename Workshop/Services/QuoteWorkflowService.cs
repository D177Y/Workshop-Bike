using System.Net;
using Workshop.Models;

namespace Workshop.Services;

public sealed class QuoteWorkflowService
{
    private readonly WorkshopData _data;
    private readonly CustomerProfileService _customerProfiles;
    private readonly EmailRetryQueueService _retryQueue;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<QuoteWorkflowService> _logger;

    public QuoteWorkflowService(
        WorkshopData data,
        CustomerProfileService customerProfiles,
        EmailRetryQueueService retryQueue,
        IEmailSender emailSender,
        ILogger<QuoteWorkflowService> logger)
    {
        _data = data;
        _customerProfiles = customerProfiles;
        _retryQueue = retryQueue;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<CustomerProfile?> SaveQuoteAsync(string accountNumber, CustomerQuoteRecord quote)
    {
        await _data.EnsureInitializedAsync();
        var account = (accountNumber ?? "").Trim();
        if (string.IsNullOrWhiteSpace(account))
            return null;

        if (!quote.ExpiresAtUtc.HasValue)
            quote.ExpiresAtUtc = DateTime.UtcNow.AddDays(30);
        quote.Status = QuoteLifecycleStatus.Draft;
        quote.StatusUpdatedUtc = DateTime.UtcNow;

        return await _data.AppendQuoteAsync(account, quote);
    }

    public async Task<QuoteEmailDispatchResult> EmailQuoteAsync(QuoteEmailDispatchRequest request)
    {
        var sent = new List<string>();
        var failed = new List<string>();

        var quoteSummary = $"Quote {request.Quote.Id} - GBP {request.Quote.EstimatedPriceIncVat:0.00}";
        var (htmlBody, textBody) = BuildQuoteEmailBodies(request, request.Quote);
        var customerName = string.Join(" ", new[] { request.CustomerFirstName, request.CustomerLastName }
            .Where(v => !string.IsNullOrWhiteSpace(v)))
            .Trim();

        var customerEmail = request.CustomerEmail.Trim();
        if (!string.IsNullOrWhiteSpace(customerEmail))
        {
            await SendWithReliabilityAsync(
                request.CustomerAccountNumber,
                customerEmail,
                $"Workshop quote for {customerName}".Trim(),
                htmlBody,
                textBody,
                quoteSummary,
                "Assessment quote",
                sent,
                failed);
        }

        var bookingEmail = request.BookingEmail.Trim();
        if (!string.IsNullOrWhiteSpace(bookingEmail))
        {
            await SendWithReliabilityAsync(
                request.CustomerAccountNumber,
                bookingEmail,
                $"Assessment quote ready - {customerName}".Trim(),
                htmlBody,
                textBody,
                quoteSummary,
                "Assessment booking copy",
                sent,
                failed);
        }

        if (sent.Count > 0)
        {
            await MarkQuoteSentAsync(
                request.CustomerAccountNumber,
                request.Quote.Id,
                $"Sent to {string.Join(" and ", sent)}");
        }
        else if (failed.Count > 0)
        {
            await _data.UpdateQuoteStatusAsync(
                request.CustomerAccountNumber,
                request.Quote.Id,
                QuoteLifecycleStatus.Draft,
                "Email queued for retry.");
        }

        return new QuoteEmailDispatchResult(sent, failed);
    }

    public Task<CustomerProfile?> MarkQuoteSentAsync(string accountNumber, string quoteId, string? detail = null) =>
        _data.UpdateQuoteStatusAsync(accountNumber, quoteId, QuoteLifecycleStatus.Sent, detail);

    public Task<CustomerProfile?> MarkQuoteAcceptedAsync(string accountNumber, string quoteId, int bookingId) =>
        _data.UpdateQuoteStatusAsync(
            accountNumber,
            quoteId,
            QuoteLifecycleStatus.Accepted,
            bookingId > 0 ? $"Accepted into booking #{bookingId}" : "Accepted into booking");

    private static (string HtmlBody, string TextBody) BuildQuoteEmailBodies(
        QuoteEmailDispatchRequest request,
        CustomerQuoteRecord quote)
    {
        var customerName = string.Join(" ", new[] { request.CustomerFirstName, request.CustomerLastName }
            .Where(v => !string.IsNullOrWhiteSpace(v)))
            .Trim();
        if (string.IsNullOrWhiteSpace(customerName))
            customerName = request.CustomerAccountNumber.Trim();

        var textLines = new List<string>
        {
            $"Quote reference: {quote.Id}",
            $"Customer: {customerName}",
            $"Account: {request.CustomerAccountNumber}",
            $"Store: {(string.IsNullOrWhiteSpace(quote.StoreName) ? "-" : quote.StoreName)}",
            $"Created: {quote.CreatedUtc.ToLocalTime():dd MMM yyyy HH:mm}",
            $"Bike: {(string.IsNullOrWhiteSpace(quote.BikeDetails) ? "-" : quote.BikeDetails)}",
            $"Services: {(quote.JobNames.Length == 0 ? "-" : string.Join(", ", quote.JobNames))}",
            $"Estimated duration: {quote.EstimatedMinutes} mins",
            $"Estimated price: GBP {quote.EstimatedPriceIncVat:0.00}",
            "",
            "Assessment notes:",
            string.IsNullOrWhiteSpace(quote.Notes) ? "-" : quote.Notes
        };

        var textBody = string.Join("\n", textLines);
        var htmlBody = "<p>" + WebUtility.HtmlEncode(textBody).Replace("\n", "<br />") + "</p>";
        return (htmlBody, textBody);
    }

    private async Task TryAppendCommunicationAsync(string accountNumber, CustomerCommunicationRecord communication)
    {
        var account = (accountNumber ?? "").Trim();
        if (string.IsNullOrWhiteSpace(account))
            return;

        try
        {
            await _customerProfiles.AppendCommunicationAsync(account, communication);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to append customer communication for account {AccountNumber}", account);
        }
    }

    private async Task SendWithReliabilityAsync(
        string accountNumber,
        string recipient,
        string subject,
        string htmlBody,
        string textBody,
        string summary,
        string source,
        List<string> sent,
        List<string> failed)
    {
        try
        {
            await _emailSender.SendAsync(recipient, subject, htmlBody, textBody);
            sent.Add($"{sourceLabel(source)} ({recipient})");
            await TryAppendCommunicationAsync(accountNumber, new CustomerCommunicationRecord
            {
                SentAtUtc = DateTime.UtcNow,
                Channel = "Email",
                Direction = "Outbound",
                Recipient = recipient,
                Summary = summary,
                DeliveryStatus = "Sent",
                DeliveryError = "",
                IsAutomated = false,
                Source = source
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send quote email to {Recipient}", recipient);
            failed.Add($"{sourceLabel(source)} ({recipient})");

            await _retryQueue.EnqueueAsync(accountNumber, recipient, subject, htmlBody, textBody, source);

            await TryAppendCommunicationAsync(accountNumber, new CustomerCommunicationRecord
            {
                SentAtUtc = DateTime.UtcNow,
                Channel = "Email",
                Direction = "Outbound",
                Recipient = recipient,
                Summary = $"{summary} (queued retry)",
                DeliveryStatus = "Queued",
                DeliveryError = ex.Message,
                IsAutomated = true,
                Source = source
            });
        }
    }

    private static string sourceLabel(string source)
    {
        if (source.Contains("booking", StringComparison.OrdinalIgnoreCase))
            return "booking";

        return "customer";
    }
}

public sealed record QuoteEmailDispatchRequest(
    string CustomerAccountNumber,
    string CustomerFirstName,
    string CustomerLastName,
    string CustomerEmail,
    string BookingEmail,
    CustomerQuoteRecord Quote);

public sealed record QuoteEmailDispatchResult(
    IReadOnlyList<string> Sent,
    IReadOnlyList<string> Failed);
