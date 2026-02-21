using Microsoft.EntityFrameworkCore;
using Workshop.Data;
using Workshop.Models;

namespace Workshop.Services;

public sealed class EmailRetryQueueService
{
    private readonly IDbContextFactory<WorkshopDbContext> _factory;
    private readonly TenantContext _tenantContext;

    public EmailRetryQueueService(IDbContextFactory<WorkshopDbContext> factory, TenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public async Task EnqueueAsync(string accountNumber, string recipient, string subject, string htmlBody, string textBody, string source)
    {
        var account = (accountNumber ?? "").Trim();
        var to = (recipient ?? "").Trim();
        if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(to))
            return;

        await using var db = await _factory.CreateDbContextAsync();
        db.EmailRetryQueueItems.Add(new EmailRetryQueueItem
        {
            TenantId = _tenantContext.TenantId,
            AccountNumber = account,
            Recipient = to,
            Subject = (subject ?? "").Trim(),
            HtmlBody = htmlBody ?? "",
            TextBody = textBody ?? "",
            Source = string.IsNullOrWhiteSpace(source) ? "Email retry" : source,
            AttemptCount = 0,
            MaxAttempts = 5,
            CreatedUtc = DateTime.UtcNow,
            NextAttemptUtc = DateTime.UtcNow,
            Status = EmailRetryStatus.Pending
        });
        await db.SaveChangesAsync();
    }

    public async Task<EmailRetryQueueStats> GetStatsAsync()
    {
        var tenantId = _tenantContext.TenantId;
        await using var db = await _factory.CreateDbContextAsync();

        var pending = await db.EmailRetryQueueItems.CountAsync(x =>
            x.TenantId == tenantId && x.Status == EmailRetryStatus.Pending);
        var deadLetter = await db.EmailRetryQueueItems.CountAsync(x =>
            x.TenantId == tenantId && x.Status == EmailRetryStatus.DeadLetter);
        var sent = await db.EmailRetryQueueItems.CountAsync(x =>
            x.TenantId == tenantId && x.Status == EmailRetryStatus.Sent);

        return new EmailRetryQueueStats(pending, deadLetter, sent);
    }

    public async Task<List<EmailRetryQueueItem>> ListAsync(string statusFilter, int take)
    {
        var tenantId = _tenantContext.TenantId;
        var normalizedTake = Math.Clamp(take, 1, 400);
        var filter = NormalizeStatusFilter(statusFilter);

        await using var db = await _factory.CreateDbContextAsync();
        var query = db.EmailRetryQueueItems
            .Where(x => x.TenantId == tenantId);

        if (!filter.Equals("all", StringComparison.OrdinalIgnoreCase))
            query = query.Where(x => x.Status == filter);

        return await query
            .OrderByDescending(x => x.CreatedUtc)
            .Take(normalizedTake)
            .ToListAsync();
    }

    public async Task<bool> RequeueNowAsync(int id)
    {
        var tenantId = _tenantContext.TenantId;
        await using var db = await _factory.CreateDbContextAsync();
        var item = await db.EmailRetryQueueItems.FirstOrDefaultAsync(x =>
            x.Id == id && x.TenantId == tenantId);
        if (item is null)
            return false;

        item.Status = EmailRetryStatus.Pending;
        item.NextAttemptUtc = DateTime.UtcNow;
        item.LastError = "";
        if (item.MaxAttempts < 1)
            item.MaxAttempts = 5;

        db.EmailRetryQueueItems.Update(item);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MarkDeadLetterAsync(int id, string? reason = null)
    {
        var tenantId = _tenantContext.TenantId;
        await using var db = await _factory.CreateDbContextAsync();
        var item = await db.EmailRetryQueueItems.FirstOrDefaultAsync(x =>
            x.Id == id && x.TenantId == tenantId);
        if (item is null)
            return false;

        item.Status = EmailRetryStatus.DeadLetter;
        item.LastError = string.IsNullOrWhiteSpace(reason) ? item.LastError : reason.Trim();
        item.NextAttemptUtc = DateTime.UtcNow.AddYears(5);

        db.EmailRetryQueueItems.Update(item);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<int> PurgeSentOlderThanDaysAsync(int days)
    {
        var tenantId = _tenantContext.TenantId;
        var threshold = DateTime.UtcNow.AddDays(-Math.Clamp(days, 1, 3650));

        await using var db = await _factory.CreateDbContextAsync();
        var rows = await db.EmailRetryQueueItems
            .Where(x => x.TenantId == tenantId
                        && x.Status == EmailRetryStatus.Sent
                        && x.SentUtc.HasValue
                        && x.SentUtc.Value < threshold)
            .ToListAsync();

        if (rows.Count == 0)
            return 0;

        db.EmailRetryQueueItems.RemoveRange(rows);
        await db.SaveChangesAsync();
        return rows.Count;
    }

    private static string NormalizeStatusFilter(string statusFilter)
    {
        var status = (statusFilter ?? "").Trim();
        if (string.IsNullOrWhiteSpace(status))
            return "all";

        if (status.Equals(EmailRetryStatus.Pending, StringComparison.OrdinalIgnoreCase))
            return EmailRetryStatus.Pending;
        if (status.Equals(EmailRetryStatus.Sent, StringComparison.OrdinalIgnoreCase))
            return EmailRetryStatus.Sent;
        if (status.Equals(EmailRetryStatus.DeadLetter, StringComparison.OrdinalIgnoreCase))
            return EmailRetryStatus.DeadLetter;

        return "all";
    }
}

public sealed record EmailRetryQueueStats(int Pending, int DeadLetter, int Sent);
