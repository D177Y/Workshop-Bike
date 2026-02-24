using Microsoft.EntityFrameworkCore;
using Workshop.Data;
using Workshop.Models;

namespace Workshop.Services;

public sealed class TrialLifecycleService
{
    public const int TrialDays = 14;
    public const int UpgradeWindowDays = 7;
    public const int FeedbackWindowDays = 7;

    private readonly IDbContextFactory<WorkshopDbContext> _dbFactory;

    public TrialLifecycleService(IDbContextFactory<WorkshopDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<TrialLifecycleStatus> GetForTenantAsync(int tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId <= 0)
            return TrialLifecycleStatus.Unknown;

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var tenant = await db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        if (tenant is null)
            return TrialLifecycleStatus.Unknown;

        return Evaluate(tenant);
    }

    public TrialLifecycleStatus Evaluate(Tenant tenant)
        => Evaluate(tenant, DateTime.UtcNow);

    public static TrialLifecycleStatus Evaluate(Tenant tenant, DateTime nowUtc)
    {
        if (tenant.HasActivatedSubscription)
        {
            return new TrialLifecycleStatus(
                TrialLifecyclePhase.Paid,
                tenant.CreatedAtUtc.AddDays(TrialDays),
                tenant.CreatedAtUtc.AddDays(TrialDays + UpgradeWindowDays),
                tenant.CreatedAtUtc.AddDays(TotalRetentionDays),
                0,
                0,
                0,
                false,
                false,
                StripeBillingPolicy.HasBillableAccess(tenant),
                false,
                true);
        }

        var trialEndsUtc = tenant.CreatedAtUtc.AddDays(TrialDays);
        var upgradeWindowEndsUtc = trialEndsUtc.AddDays(UpgradeWindowDays);
        var feedbackWindowEndsUtc = upgradeWindowEndsUtc.AddDays(FeedbackWindowDays);
        var daysUntilTrialEnd = DaysRemaining(nowUtc, trialEndsUtc);
        var daysUntilUpgradeWindowEnd = DaysRemaining(nowUtc, upgradeWindowEndsUtc);
        var daysUntilFeedbackWindowEnd = DaysRemaining(nowUtc, feedbackWindowEndsUtc);

        if (StripeBillingPolicy.HasBillableAccess(tenant))
        {
            return new TrialLifecycleStatus(
                TrialLifecyclePhase.Paid,
                trialEndsUtc,
                upgradeWindowEndsUtc,
                feedbackWindowEndsUtc,
                daysUntilTrialEnd,
                daysUntilUpgradeWindowEnd,
                daysUntilFeedbackWindowEnd,
                false,
                false,
                false,
                false,
                false);
        }

        if (tenant.TrialDataPurgedAtUtc.HasValue || nowUtc >= feedbackWindowEndsUtc)
        {
            return new TrialLifecycleStatus(
                TrialLifecyclePhase.Purged,
                trialEndsUtc,
                upgradeWindowEndsUtc,
                feedbackWindowEndsUtc,
                daysUntilTrialEnd,
                daysUntilUpgradeWindowEnd,
                daysUntilFeedbackWindowEnd,
                true,
                false,
                true,
                false,
                true);
        }

        if (nowUtc < trialEndsUtc)
        {
            return new TrialLifecycleStatus(
                TrialLifecyclePhase.ActiveTrial,
                trialEndsUtc,
                upgradeWindowEndsUtc,
                feedbackWindowEndsUtc,
                daysUntilTrialEnd,
                daysUntilUpgradeWindowEnd,
                daysUntilFeedbackWindowEnd,
                false,
                false,
                true,
                false,
                false);
        }

        if (nowUtc < upgradeWindowEndsUtc)
        {
            return new TrialLifecycleStatus(
                TrialLifecyclePhase.UpgradeWindow,
                trialEndsUtc,
                upgradeWindowEndsUtc,
                feedbackWindowEndsUtc,
                daysUntilTrialEnd,
                daysUntilUpgradeWindowEnd,
                daysUntilFeedbackWindowEnd,
                true,
                false,
                true,
                false,
                false);
        }

        return new TrialLifecycleStatus(
            TrialLifecyclePhase.FeedbackWindow,
            trialEndsUtc,
            upgradeWindowEndsUtc,
            feedbackWindowEndsUtc,
            daysUntilTrialEnd,
            daysUntilUpgradeWindowEnd,
            daysUntilFeedbackWindowEnd,
            true,
            true,
            true,
            true,
            false);
    }

    public static int TotalRetentionDays => TrialDays + UpgradeWindowDays + FeedbackWindowDays;

    private static int DaysRemaining(DateTime nowUtc, DateTime targetUtc)
    {
        var remaining = targetUtc - nowUtc;
        if (remaining <= TimeSpan.Zero)
            return 0;
        return (int)Math.Ceiling(remaining.TotalDays);
    }
}

public enum TrialLifecyclePhase
{
    Unknown = 0,
    Paid = 1,
    ActiveTrial = 2,
    UpgradeWindow = 3,
    FeedbackWindow = 4,
    Purged = 5
}

public sealed record TrialLifecycleStatus(
    TrialLifecyclePhase Phase,
    DateTime TrialEndsUtc,
    DateTime UpgradeWindowEndsUtc,
    DateTime FeedbackWindowEndsUtc,
    int DaysUntilTrialEnd,
    int DaysUntilUpgradeWindowEnd,
    int DaysUntilFeedbackWindowEnd,
    bool RequiresAppGate,
    bool ShowFeedbackForm,
    bool CanUpgrade,
    bool CanSubmitFeedback,
    bool TrialAlreadyUsed)
{
    public static readonly TrialLifecycleStatus Unknown = new(
        TrialLifecyclePhase.Unknown,
        DateTime.MinValue,
        DateTime.MinValue,
        DateTime.MinValue,
        0,
        0,
        0,
        false,
        false,
        false,
        false,
        false);
}
