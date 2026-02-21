using Microsoft.EntityFrameworkCore;
using Workshop.Data;
using Workshop.Models;

namespace Workshop.Services;

public sealed class FinancialYearService
{
    private readonly IDbContextFactory<WorkshopDbContext> _dbFactory;
    private readonly TenantContext _tenantContext;

    public FinancialYearService(IDbContextFactory<WorkshopDbContext> dbFactory, TenantContext tenantContext)
    {
        _dbFactory = dbFactory;
        _tenantContext = tenantContext;
    }

    public async Task<FinancialYearBoundarySettings> GetSettingsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == _tenantContext.TenantId);
        if (tenant is null)
            return FinancialYearBoundarySettings.Default;

        return Normalize(new FinancialYearBoundarySettings(
            tenant.FinancialYearStartMonth,
            tenant.FinancialYearStartDay,
            tenant.FinancialYearEndMonth,
            tenant.FinancialYearEndDay));
    }

    public async Task<FinancialYearBoundarySettings> SaveSettingsAsync(FinancialYearBoundarySettings settings)
    {
        var normalized = Normalize(settings);

        await using var db = await _dbFactory.CreateDbContextAsync();
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == _tenantContext.TenantId);
        if (tenant is null)
            throw new InvalidOperationException("Tenant not found.");

        tenant.FinancialYearStartMonth = normalized.StartMonth;
        tenant.FinancialYearStartDay = normalized.StartDay;
        tenant.FinancialYearEndMonth = normalized.EndMonth;
        tenant.FinancialYearEndDay = normalized.EndDay;

        await db.SaveChangesAsync();
        return normalized;
    }

    public async Task<FinancialYearRange> GetCurrentRangeAsync(DateTime? referenceDate = null)
    {
        var settings = await GetSettingsAsync();
        return ResolveRange(settings, referenceDate ?? DateTime.Today);
    }

    public static FinancialYearRange ResolveRange(FinancialYearBoundarySettings settings, DateTime referenceDate)
    {
        var normalized = Normalize(settings);
        var reference = referenceDate.Date;

        var startThisYear = CreateDate(reference.Year, normalized.StartMonth, normalized.StartDay);
        var startYear = reference < startThisYear ? reference.Year - 1 : reference.Year;
        var start = CreateDate(startYear, normalized.StartMonth, normalized.StartDay);

        var endYear = startYear + (IsEndInFollowingYear(normalized) ? 1 : 0);
        var end = CreateDate(endYear, normalized.EndMonth, normalized.EndDay);
        if (end < start)
            end = end.AddYears(1);

        return new FinancialYearRange(start, end);
    }

    public static FinancialYearBoundarySettings Normalize(FinancialYearBoundarySettings settings)
    {
        var startMonth = Clamp(settings.StartMonth, 1, 12);
        var endMonth = Clamp(settings.EndMonth, 1, 12);
        var startDay = Clamp(settings.StartDay, 1, DateTime.DaysInMonth(2025, startMonth));
        var endDay = Clamp(settings.EndDay, 1, DateTime.DaysInMonth(2025, endMonth));
        return new FinancialYearBoundarySettings(startMonth, startDay, endMonth, endDay);
    }

    public static bool IsEndInFollowingYear(FinancialYearBoundarySettings settings)
    {
        if (settings.EndMonth < settings.StartMonth)
            return true;

        return settings.EndMonth == settings.StartMonth && settings.EndDay < settings.StartDay;
    }

    public static DateTime CreateDate(int year, int month, int day)
    {
        var safeMonth = Clamp(month, 1, 12);
        var safeDay = Clamp(day, 1, DateTime.DaysInMonth(year, safeMonth));
        return new DateTime(year, safeMonth, safeDay);
    }

    private static int Clamp(int value, int min, int max)
    {
        if (value < min)
            return min;
        if (value > max)
            return max;
        return value;
    }
}

public readonly record struct FinancialYearBoundarySettings(int StartMonth, int StartDay, int EndMonth, int EndDay)
{
    public static FinancialYearBoundarySettings Default => new(1, 1, 12, 31);
}

public readonly record struct FinancialYearRange(DateTime StartDate, DateTime EndDate)
{
    public bool Contains(DateTime date) => date.Date >= StartDate.Date && date.Date <= EndDate.Date;
}
