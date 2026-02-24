using Microsoft.EntityFrameworkCore;
using Workshop.Data;

namespace Workshop.Services;

public sealed class TrialDataPurgeBackgroundService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(6);
    private const int BatchSize = 20;

    private readonly IDbContextFactory<WorkshopDbContext> _dbFactory;
    private readonly OperationalAlertService _alerts;
    private readonly ILogger<TrialDataPurgeBackgroundService> _logger;

    public TrialDataPurgeBackgroundService(
        IDbContextFactory<WorkshopDbContext> dbFactory,
        OperationalAlertService alerts,
        ILogger<TrialDataPurgeBackgroundService> logger)
    {
        _dbFactory = dbFactory;
        _alerts = alerts;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PurgeExpiredTrialsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Trial purge worker iteration failed.");
                await _alerts.NotifyAsync(
                    "trial-purge",
                    "Workshop alert: Trial purge worker failed",
                    "The trial purge background worker failed.",
                    ex,
                    stoppingToken);
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

    private async Task PurgeExpiredTrialsAsync(CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;
        var purgeThresholdUtc = nowUtc.AddDays(-TrialLifecycleService.TotalRetentionDays);

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var dueTenantIds = await db.Tenants
            .Where(t =>
                !t.HasActivatedSubscription
                && !t.TrialDataPurgedAtUtc.HasValue
                && t.CreatedAtUtc <= purgeThresholdUtc)
            .OrderBy(t => t.CreatedAtUtc)
            .Select(t => t.Id)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (dueTenantIds.Count == 0)
            return;

        foreach (var tenantId in dueTenantIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await PurgeTenantAsync(db, tenantId, nowUtc, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task PurgeTenantAsync(
        WorkshopDbContext db,
        int tenantId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var storeIds = await db.Stores
            .Where(x => x.TenantId == tenantId)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var mechanicIds = await db.Mechanics
            .Where(x => x.TenantId == tenantId)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var userIds = await db.Users
            .Where(x => x.TenantId == tenantId)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (storeIds.Count > 0 || userIds.Count > 0)
        {
            await db.UserStoreAccess
                .Where(x => storeIds.Contains(x.StoreId) || userIds.Contains(x.UserId))
                .ExecuteDeleteAsync(cancellationToken);
        }

        if (mechanicIds.Count > 0 || userIds.Count > 0)
        {
            await db.UserMechanicAccess
                .Where(x => mechanicIds.Contains(x.MechanicId) || userIds.Contains(x.UserId))
                .ExecuteDeleteAsync(cancellationToken);
        }

        await db.MechanicTimeOffEntries.Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);
        await db.EmailRetryQueueItems.Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);
        await db.CustomerProfiles.Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);
        await db.TimetasticWebhookEvents.Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);
        await db.IntegrationSettings.Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);
        await db.Bookings.Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);
        await db.BookingStatuses.Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);
        await db.AddOnRules.Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);
        await db.AddOnDefinitions.Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);
        await db.JobDefinitions.Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);
        await db.Mechanics.Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);
        await db.Stores.Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);
        await db.CatalogSettings.Where(x => x.TenantId == tenantId).ExecuteDeleteAsync(cancellationToken);

        var tenant = await db.Tenants.FirstOrDefaultAsync(x => x.Id == tenantId, cancellationToken);
        if (tenant is null)
            return;

        tenant.IsActive = false;
        tenant.TrialDataPurgedAtUtc = nowUtc;
        tenant.StripeCustomerId = "";
        tenant.StripeSubscriptionId = "";
        tenant.StripeSubscriptionStatus = "";
        tenant.StripeCurrentPeriodEndUtc = null;
        tenant.StripeSubscriptionUpdatedAtUtc = nowUtc;

        _logger.LogInformation("Purged tenant operational data for tenant {TenantId}.", tenantId);
    }
}
