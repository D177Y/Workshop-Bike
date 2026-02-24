using Microsoft.EntityFrameworkCore;
using Workshop.Data;
using Workshop.Models;

namespace Workshop.Services;

public sealed class SuperAdminDashboardService
{
    private readonly IDbContextFactory<WorkshopDbContext> _dbFactory;

    public SuperAdminDashboardService(IDbContextFactory<WorkshopDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<SuperAdminDashboardData> GetSnapshotAsync(int monthCount = 12, CancellationToken cancellationToken = default)
    {
        var safeMonthCount = Math.Clamp(monthCount, 6, 24);
        var nowUtc = DateTime.UtcNow;

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var tenants = await db.Tenants
            .AsNoTracking()
            .Where(t => t.Id > 0)
            .ToListAsync(cancellationToken);

        var userCountsByTenant = await db.Users
            .AsNoTracking()
            .Where(u => u.TenantId > 0)
            .GroupBy(u => u.TenantId)
            .Select(group => new TenantUserCount(
                group.Key,
                group.Count(),
                group.Count(u => u.IsActive)))
            .ToListAsync(cancellationToken);

        var userCountsLookup = userCountsByTenant.ToDictionary(x => x.TenantId);

        var lifecycleByTenant = tenants
            .Select(t => (Tenant: t, Lifecycle: TrialLifecycleService.Evaluate(t, nowUtc)))
            .ToList();

        var totalTenants = tenants.Count;
        var activeTrialTenants = lifecycleByTenant.Count(x => x.Lifecycle.Phase == TrialLifecyclePhase.ActiveTrial);
        var upgradeWindowTenants = lifecycleByTenant.Count(x => x.Lifecycle.Phase == TrialLifecyclePhase.UpgradeWindow);
        var feedbackWindowTenants = lifecycleByTenant.Count(x => x.Lifecycle.Phase == TrialLifecyclePhase.FeedbackWindow);

        var paidTenants = tenants
            .Where(StripeBillingPolicy.HasBillableAccess)
            .ToList();

        var paidTenantCount = paidTenants.Count;
        var paidTierCounts = BuildPaidTierCounts(paidTenants);
        var estimatedMrr = paidTierCounts.Sum(x => x.TenantCount * PlanCatalog.Get(x.Tier).MonthlyPrice);

        var billingAttentionTenants = tenants.Count(t =>
            t.HasActivatedSubscription &&
            !StripeBillingPolicy.HasBillableAccess(t));

        var totalUsers = userCountsByTenant.Sum(x => x.TotalUsers);
        var activeUsers = userCountsByTenant.Sum(x => x.ActiveUsers);
        var averageUsersPerTenant = totalTenants > 0
            ? decimal.Round((decimal)totalUsers / totalTenants, 2, MidpointRounding.AwayFromZero)
            : 0m;
        var tenantsWithoutActiveUsers = tenants.Count(t =>
            !userCountsLookup.TryGetValue(t.Id, out var counts) || counts.ActiveUsers == 0);

        var thirtyDaysAgoUtc = nowUtc.AddDays(-30);
        var newTenantsLast30Days = tenants.Count(t => t.CreatedAtUtc >= thirtyDaysAgoUtc);
        var newPaidTenantsLast30Days = tenants.Count(t =>
        {
            var activationDate = ResolvePaidActivationDateUtc(t);
            return activationDate.HasValue && activationDate.Value >= thirtyDaysAgoUtc;
        });

        var conversionDenominator = totalTenants == 0 ? 1 : totalTenants;
        var trialToPaidConversionRatePercent = decimal.Round(
            paidTenantCount * 100m / conversionDenominator,
            1,
            MidpointRounding.AwayFromZero);

        var trend = BuildMonthlyTrend(tenants, safeMonthCount, nowUtc);
        var recentTenants = BuildRecentTenants(tenants, userCountsLookup, nowUtc);

        return new SuperAdminDashboardData(
            GeneratedAtUtc: nowUtc,
            TotalTenants: totalTenants,
            ActiveTrialTenants: activeTrialTenants,
            UpgradeWindowTenants: upgradeWindowTenants,
            FeedbackWindowTenants: feedbackWindowTenants,
            PaidTenantCount: paidTenantCount,
            BillingAttentionTenants: billingAttentionTenants,
            TotalUsers: totalUsers,
            ActiveUsers: activeUsers,
            AverageUsersPerTenant: averageUsersPerTenant,
            TenantsWithoutActiveUsers: tenantsWithoutActiveUsers,
            NewTenantsLast30Days: newTenantsLast30Days,
            NewPaidTenantsLast30Days: newPaidTenantsLast30Days,
            TrialToPaidConversionRatePercent: trialToPaidConversionRatePercent,
            EstimatedMrr: estimatedMrr,
            PaidTierCounts: paidTierCounts,
            Trend: trend,
            RecentTenants: recentTenants);
    }

    private static List<SuperAdminPaidTierCount> BuildPaidTierCounts(IReadOnlyCollection<Tenant> paidTenants)
    {
        var countsByTier = paidTenants
            .GroupBy(t => t.Plan)
            .ToDictionary(group => group.Key, group => group.Count());

        return PlanCatalog.Ordered
            .Select(plan => new SuperAdminPaidTierCount(
                plan.Tier,
                plan.Name,
                countsByTier.TryGetValue(plan.Tier, out var count) ? count : 0))
            .ToList();
    }

    private static List<SuperAdminTrendPoint> BuildMonthlyTrend(IReadOnlyCollection<Tenant> tenants, int monthCount, DateTime nowUtc)
    {
        var monthStartUtc = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var firstMonthUtc = monthStartUtc.AddMonths(-(monthCount - 1));

        var trend = new List<SuperAdminTrendPoint>(monthCount);
        for (var index = 0; index < monthCount; index++)
        {
            var currentMonthStart = firstMonthUtc.AddMonths(index);
            var nextMonthStart = currentMonthStart.AddMonths(1);
            var monthEnd = nextMonthStart.AddTicks(-1);

            var newTenants = tenants.Count(t =>
                t.CreatedAtUtc >= currentMonthStart &&
                t.CreatedAtUtc < nextMonthStart);

            var newPaid = tenants.Count(t =>
            {
                var activationDate = ResolvePaidActivationDateUtc(t);
                return activationDate.HasValue
                    && activationDate.Value >= currentMonthStart
                    && activationDate.Value < nextMonthStart;
            });

            var totalTenants = tenants.Count(t => t.CreatedAtUtc <= monthEnd);
            var totalPaid = tenants.Count(t =>
            {
                var activationDate = ResolvePaidActivationDateUtc(t);
                return activationDate.HasValue && activationDate.Value <= monthEnd;
            });

            var activeTrials = tenants.Count(t =>
                TrialLifecycleService.Evaluate(t, monthEnd).Phase == TrialLifecyclePhase.ActiveTrial);

            trend.Add(new SuperAdminTrendPoint
            {
                Month = currentMonthStart,
                NewTenants = newTenants,
                NewPaidTenants = newPaid,
                TotalTenants = totalTenants,
                TotalPaidTenants = totalPaid,
                ActiveTrialTenants = activeTrials
            });
        }

        return trend;
    }

    private static List<SuperAdminRecentTenantRow> BuildRecentTenants(
        IReadOnlyCollection<Tenant> tenants,
        IReadOnlyDictionary<int, TenantUserCount> userCountsLookup,
        DateTime nowUtc)
    {
        return tenants
            .OrderByDescending(t => t.CreatedAtUtc)
            .Take(10)
            .Select(t =>
            {
                userCountsLookup.TryGetValue(t.Id, out var counts);
                var lifecycle = TrialLifecycleService.Evaluate(t, nowUtc);
                var lifecycleLabel = lifecycle.Phase switch
                {
                    TrialLifecyclePhase.ActiveTrial => "Free trial",
                    TrialLifecyclePhase.UpgradeWindow => "Upgrade window",
                    TrialLifecyclePhase.FeedbackWindow => "Feedback window",
                    TrialLifecyclePhase.Purged => "Purged",
                    TrialLifecyclePhase.Paid => "Paid",
                    _ => "Unknown"
                };

                var normalizedStatus = StripeBillingPolicy.NormalizeStatus(t.StripeSubscriptionStatus);
                var billingStatus = string.IsNullOrWhiteSpace(normalizedStatus)
                    ? (StripeBillingPolicy.HasBillableAccess(t) ? "active" : "none")
                    : normalizedStatus;

                return new SuperAdminRecentTenantRow(
                    TenantId: t.Id,
                    Name: string.IsNullOrWhiteSpace(t.Name) ? $"Tenant {t.Id}" : t.Name.Trim(),
                    CreatedAtUtc: t.CreatedAtUtc,
                    Plan: t.Plan,
                    Lifecycle: lifecycleLabel,
                    BillingStatus: billingStatus,
                    ActiveUserCount: counts?.ActiveUsers ?? 0);
            })
            .ToList();
    }

    private static DateTime? ResolvePaidActivationDateUtc(Tenant tenant)
    {
        if (!tenant.HasActivatedSubscription && !StripeBillingPolicy.HasBillableAccess(tenant))
            return null;

        if (tenant.StripeSubscriptionUpdatedAtUtc.HasValue)
            return tenant.StripeSubscriptionUpdatedAtUtc.Value;

        return tenant.CreatedAtUtc;
    }

    private sealed record TenantUserCount(int TenantId, int TotalUsers, int ActiveUsers);
}

public sealed record SuperAdminDashboardData(
    DateTime GeneratedAtUtc,
    int TotalTenants,
    int ActiveTrialTenants,
    int UpgradeWindowTenants,
    int FeedbackWindowTenants,
    int PaidTenantCount,
    int BillingAttentionTenants,
    int TotalUsers,
    int ActiveUsers,
    decimal AverageUsersPerTenant,
    int TenantsWithoutActiveUsers,
    int NewTenantsLast30Days,
    int NewPaidTenantsLast30Days,
    decimal TrialToPaidConversionRatePercent,
    decimal EstimatedMrr,
    List<SuperAdminPaidTierCount> PaidTierCounts,
    List<SuperAdminTrendPoint> Trend,
    List<SuperAdminRecentTenantRow> RecentTenants);

public sealed record SuperAdminPaidTierCount(
    PlanTier Tier,
    string Label,
    int TenantCount);

public sealed class SuperAdminTrendPoint
{
    public DateTime Month { get; set; }
    public int NewTenants { get; set; }
    public int NewPaidTenants { get; set; }
    public int TotalTenants { get; set; }
    public int TotalPaidTenants { get; set; }
    public int ActiveTrialTenants { get; set; }
}

public sealed record SuperAdminRecentTenantRow(
    int TenantId,
    string Name,
    DateTime CreatedAtUtc,
    PlanTier Plan,
    string Lifecycle,
    string BillingStatus,
    int ActiveUserCount);
