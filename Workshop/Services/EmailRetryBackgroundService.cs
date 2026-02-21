using Microsoft.EntityFrameworkCore;
using Workshop.Data;
using Workshop.Models;

namespace Workshop.Services;

public sealed class EmailRetryBackgroundService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);
    private const int BatchSize = 20;

    private readonly IDbContextFactory<WorkshopDbContext> _factory;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<EmailRetryBackgroundService> _logger;

    public EmailRetryBackgroundService(
        IDbContextFactory<WorkshopDbContext> factory,
        IEmailSender emailSender,
        ILogger<EmailRetryBackgroundService> logger)
    {
        _factory = factory;
        _emailSender = emailSender;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Email retry worker iteration failed.");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task ProcessPendingAsync(CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var batch = await db.EmailRetryQueueItems
            .Where(x => x.Status == EmailRetryStatus.Pending && x.NextAttemptUtc <= nowUtc)
            .OrderBy(x => x.NextAttemptUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (batch.Count == 0)
            return;

        foreach (var item in batch)
        {
            cancellationToken.ThrowIfCancellationRequested();
            item.AttemptCount += 1;
            item.LastAttemptUtc = DateTime.UtcNow;

            try
            {
                await _emailSender.SendAsync(item.Recipient, item.Subject, item.HtmlBody, item.TextBody);
                item.Status = EmailRetryStatus.Sent;
                item.SentUtc = DateTime.UtcNow;
                item.LastError = "";

                await AppendCommunicationAsync(db, item, new CustomerCommunicationRecord
                {
                    SentAtUtc = DateTime.UtcNow,
                    Channel = "Email",
                    Direction = "Outbound",
                    Recipient = item.Recipient,
                    Summary = $"Retry delivery succeeded: {item.Subject}",
                    DeliveryStatus = "Sent",
                    DeliveryError = "",
                    IsAutomated = true,
                    Source = item.Source
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                var isDead = item.AttemptCount >= item.MaxAttempts;
                item.LastError = Truncate(ex.Message, 600);
                if (isDead)
                {
                    item.Status = EmailRetryStatus.DeadLetter;
                    item.NextAttemptUtc = DateTime.UtcNow.AddYears(5);
                }
                else
                {
                    item.Status = EmailRetryStatus.Pending;
                    item.NextAttemptUtc = DateTime.UtcNow.Add(Backoff(item.AttemptCount));
                }

                await AppendCommunicationAsync(db, item, new CustomerCommunicationRecord
                {
                    SentAtUtc = DateTime.UtcNow,
                    Channel = "Email",
                    Direction = "Outbound",
                    Recipient = item.Recipient,
                    Summary = isDead
                        ? $"Email delivery failed permanently: {item.Subject}"
                        : $"Email delivery failed, queued retry {item.AttemptCount}/{item.MaxAttempts}: {item.Subject}",
                    DeliveryStatus = isDead ? "DeadLetter" : "Queued",
                    DeliveryError = item.LastError,
                    IsAutomated = true,
                    Source = item.Source
                }, cancellationToken);

                _logger.LogWarning(ex,
                    "Retry email send failed for queue item {QueueItemId} (attempt {Attempt}/{MaxAttempts}).",
                    item.Id,
                    item.AttemptCount,
                    item.MaxAttempts);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static TimeSpan Backoff(int attempt)
    {
        var minutes = attempt switch
        {
            <= 1 => 2,
            2 => 5,
            3 => 15,
            4 => 60,
            _ => 180
        };

        return TimeSpan.FromMinutes(minutes);
    }

    private static string Truncate(string input, int max)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "";

        return input.Length <= max ? input : input[..max];
    }

    private static async Task AppendCommunicationAsync(
        WorkshopDbContext db,
        EmailRetryQueueItem item,
        CustomerCommunicationRecord communication,
        CancellationToken cancellationToken)
    {
        var profile = await db.CustomerProfiles
            .FirstOrDefaultAsync(c =>
                c.TenantId == item.TenantId
                && c.AccountNumber == item.AccountNumber,
                cancellationToken);

        if (profile is null)
            return;

        profile.Communications ??= new List<CustomerCommunicationRecord>();
        profile.Communications.Insert(0, communication);
        if (profile.Communications.Count > 400)
            profile.Communications = profile.Communications.Take(400).ToList();

        profile.UpdatedUtc = DateTime.UtcNow;
        db.CustomerProfiles.Update(profile);
    }
}
