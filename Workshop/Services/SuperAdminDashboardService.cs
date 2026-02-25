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

    public async Task<List<SuperAdminTenantInsightRow>> GetTenantInsightsAsync(CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;
        var last30DaysUtc = nowUtc.AddDays(-30);

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var tenants = await db.Tenants
            .AsNoTracking()
            .Where(t => t.Id > 0)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

        var userInsights = await db.Users
            .AsNoTracking()
            .Where(u => u.TenantId > 0)
            .GroupBy(u => u.TenantId)
            .Select(group => new TenantUserInsight(
                group.Key,
                group.Count(),
                group.Count(u => u.IsActive),
                group.Count(u => u.EmailConfirmed),
                group.Max(u => u.LastLoginUtc)))
            .ToListAsync(cancellationToken);

        var storeCounts = await db.Stores
            .AsNoTracking()
            .Where(s => s.TenantId > 0)
            .GroupBy(s => s.TenantId)
            .Select(group => new TenantEntityCount(group.Key, group.Count()))
            .ToListAsync(cancellationToken);

        var mechanicCounts = await db.Mechanics
            .AsNoTracking()
            .Where(m => m.TenantId > 0)
            .GroupBy(m => m.TenantId)
            .Select(group => new TenantEntityCount(group.Key, group.Count()))
            .ToListAsync(cancellationToken);

        var serviceCounts = await db.JobDefinitions
            .AsNoTracking()
            .Where(j => j.TenantId > 0)
            .GroupBy(j => j.TenantId)
            .Select(group => new TenantEntityCount(group.Key, group.Count()))
            .ToListAsync(cancellationToken);

        var bookingInsights = await db.Bookings
            .AsNoTracking()
            .Where(b => b.TenantId > 0)
            .GroupBy(b => b.TenantId)
            .Select(group => new TenantBookingInsight(
                group.Key,
                group.Count(),
                group.Count(b => b.Start >= last30DaysUtc)))
            .ToListAsync(cancellationToken);

        var userLookup = userInsights.ToDictionary(x => x.TenantId);
        var storeLookup = storeCounts.ToDictionary(x => x.TenantId);
        var mechanicLookup = mechanicCounts.ToDictionary(x => x.TenantId);
        var serviceLookup = serviceCounts.ToDictionary(x => x.TenantId);
        var bookingLookup = bookingInsights.ToDictionary(x => x.TenantId);

        var rows = tenants
            .Select(tenant =>
            {
                userLookup.TryGetValue(tenant.Id, out var userInsight);
                storeLookup.TryGetValue(tenant.Id, out var storeInsight);
                mechanicLookup.TryGetValue(tenant.Id, out var mechanicInsight);
                serviceLookup.TryGetValue(tenant.Id, out var serviceInsight);
                bookingLookup.TryGetValue(tenant.Id, out var bookingInsight);

                var storeCount = storeInsight?.Count ?? 0;
                var mechanicCount = mechanicInsight?.Count ?? 0;
                var serviceCount = serviceInsight?.Count ?? 0;

                var hasStore = storeCount > 0;
                var hasMechanic = hasStore && mechanicCount > 0;
                var hasService = serviceCount > 0;
                var profileComplete = IsProfileComplete(tenant);

                var setupCompletedSteps =
                    (hasStore ? 1 : 0)
                    + (hasMechanic ? 1 : 0)
                    + (hasService ? 1 : 0)
                    + (profileComplete ? 1 : 0);

                var lifecycle = TrialLifecycleService.Evaluate(tenant, nowUtc);
                var daysLeft = lifecycle.DaysUntilTrialEnd;
                var tierDisplay = lifecycle.Phase == TrialLifecyclePhase.ActiveTrial
                    ? $"Free Trial ({daysLeft} {(daysLeft == 1 ? "day" : "days")} left)"
                    : PlanCatalog.Get(tenant.Plan).Name;

                var createdAtUtc = DateTime.SpecifyKind(tenant.CreatedAtUtc, DateTimeKind.Utc);
                var daysSinceStart = Math.Max(0, (int)(nowUtc.Date - createdAtUtc.Date).TotalDays);

                var lastLoginUtc = userInsight?.LastLoginUtc;
                int? daysSinceLastActivity = null;
                if (lastLoginUtc.HasValue)
                    daysSinceLastActivity = Math.Max(0, (int)(nowUtc.Date - lastLoginUtc.Value.Date).TotalDays);

                return new SuperAdminTenantInsightRow(
                    TenantId: tenant.Id,
                    BusinessName: string.IsNullOrWhiteSpace(tenant.Name) ? $"Tenant {tenant.Id}" : tenant.Name.Trim(),
                    IsActive: tenant.IsActive,
                    TierDisplay: tierDisplay,
                    IsTrial: lifecycle.Phase == TrialLifecyclePhase.ActiveTrial,
                    DaysSinceStart: daysSinceStart,
                    TotalUsers: userInsight?.TotalUsers ?? 0,
                    ActiveUsers: userInsight?.ActiveUsers ?? 0,
                    VerifiedUsers: userInsight?.VerifiedUsers ?? 0,
                    LastLoginUtc: lastLoginUtc,
                    DaysSinceLastActivity: daysSinceLastActivity,
                    SetupCompletedSteps: setupCompletedSteps,
                    SetupTotalSteps: 4,
                    HasStore: hasStore,
                    HasMechanic: hasMechanic,
                    HasService: hasService,
                    ProfileComplete: profileComplete,
                    BookingsTotal: bookingInsight?.TotalBookings ?? 0,
                    BookingsLast30Days: bookingInsight?.BookingsLast30Days ?? 0,
                    DataPurged: tenant.TrialDataPurgedAtUtc.HasValue,
                    DataPurgedAtUtc: tenant.TrialDataPurgedAtUtc);
            })
            .OrderByDescending(row => row.IsActive)
            .ThenBy(row => row.BusinessName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return rows;
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

    private static bool IsProfileComplete(Tenant tenant)
    {
        return !string.IsNullOrWhiteSpace(tenant.ContactName)
               && !string.IsNullOrWhiteSpace(tenant.ContactEmail)
               && !string.IsNullOrWhiteSpace(tenant.ContactPhone)
               && !string.IsNullOrWhiteSpace(tenant.AddressLine1)
               && !string.IsNullOrWhiteSpace(tenant.City)
               && !string.IsNullOrWhiteSpace(tenant.Postcode)
               && !string.IsNullOrWhiteSpace(tenant.Country);
    }

    private sealed record TenantUserCount(int TenantId, int TotalUsers, int ActiveUsers);
    private sealed record TenantUserInsight(int TenantId, int TotalUsers, int ActiveUsers, int VerifiedUsers, DateTime? LastLoginUtc);
    private sealed record TenantEntityCount(int TenantId, int Count);
    private sealed record TenantBookingInsight(int TenantId, int TotalBookings, int BookingsLast30Days);
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

public sealed record SuperAdminTenantInsightRow(
    int TenantId,
    string BusinessName,
    bool IsActive,
    string TierDisplay,
    bool IsTrial,
    int DaysSinceStart,
    int TotalUsers,
    int ActiveUsers,
    int VerifiedUsers,
    DateTime? LastLoginUtc,
    int? DaysSinceLastActivity,
    int SetupCompletedSteps,
    int SetupTotalSteps,
    bool HasStore,
    bool HasMechanic,
    bool HasService,
    bool ProfileComplete,
    int BookingsTotal,
    int BookingsLast30Days,
    bool DataPurged,
    DateTime? DataPurgedAtUtc);
