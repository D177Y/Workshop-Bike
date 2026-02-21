using Workshop.Models;

namespace Workshop.Services;

public sealed class DashboardAnalyticsService
{
    public DashboardOverviewData BuildOverview(
        IEnumerable<Booking> bookings,
        IEnumerable<Store> stores,
        IEnumerable<Mechanic> mechanics,
        IEnumerable<JobDefinition> jobs,
        DateTime startDate,
        DateTime endDate,
        UserAccessScope access)
    {
        var filtered = FilterBookings(bookings, access, startDate, endDate).ToList();
        var revenueTrend = BuildRevenueTrend(filtered, startDate, endDate);
        var topMechanics = BuildMechanicLeaderboard(filtered, stores, mechanics);
        var topStores = BuildStoreLeaderboard(filtered, stores, mechanics);
        var servicePackages = ResolveServicePackageCatalog(filtered, jobs);
        var byMechanic = BuildServicePivotByMechanic(filtered, mechanics, servicePackages);
        var byStore = BuildServicePivotByStore(filtered, stores, servicePackages);

        return new DashboardOverviewData(
            startDate.Date,
            endDate.Date,
            filtered.Sum(b => b.TotalPriceIncVat),
            revenueTrend,
            topMechanics,
            topStores,
            servicePackages.Select(p => p.Name).ToList(),
            byMechanic,
            byStore);
    }

    public List<StoreRevenueRow> BuildStoreLeaderboard(
        IEnumerable<Booking> bookings,
        IEnumerable<Store> stores,
        IEnumerable<Mechanic> mechanics)
    {
        var storeMap = stores.ToDictionary(s => s.Id);

        var rows = bookings
            .GroupBy(b => b.StoreId)
            .Select(group =>
            {
                var revenue = group.Sum(x => x.TotalPriceIncVat);
                var hours = group.Sum(GetHoursWorked);
                var storeName = storeMap.TryGetValue(group.Key, out var store) ? store.Name : $"Store {group.Key}";

                return new StoreRevenueRow
                {
                    StoreId = group.Key,
                    Store = storeName,
                    Revenue = revenue,
                    HoursWorked = hours
                };
            })
            .OrderByDescending(x => x.Revenue)
            .ThenBy(x => x.Store, StringComparer.OrdinalIgnoreCase)
            .ToList();

        for (var i = 0; i < rows.Count; i++)
            rows[i].Rank = i + 1;

        return rows;
    }

    public List<MechanicRevenueRow> BuildMechanicLeaderboard(
        IEnumerable<Booking> bookings,
        IEnumerable<Store> stores,
        IEnumerable<Mechanic> mechanics)
    {
        var storeMap = stores.ToDictionary(s => s.Id);
        var mechanicMap = mechanics.ToDictionary(m => m.Id);

        var rows = bookings
            .GroupBy(b => b.MechanicId)
            .Select(group =>
            {
                var mechanic = mechanicMap.TryGetValue(group.Key, out var value) ? value : null;
                var storeName = mechanic is null
                    ? ResolveStoreNameFromBookings(group, storeMap)
                    : (storeMap.TryGetValue(mechanic.StoreId, out var store) ? store.Name : $"Store {mechanic.StoreId}");
                var name = (mechanic?.Name ?? $"Mechanic {group.Key}").Trim();
                if (string.IsNullOrWhiteSpace(name))
                    name = $"Mechanic {group.Key}";
                var revenue = group.Sum(x => x.TotalPriceIncVat);
                var hours = group.Sum(GetHoursWorked);

                return new MechanicRevenueRow
                {
                    MechanicId = group.Key,
                    Name = name,
                    Store = storeName,
                    Revenue = revenue,
                    HoursWorked = hours
                };
            })
            .OrderByDescending(x => x.Revenue)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        for (var i = 0; i < rows.Count; i++)
            rows[i].Rank = i + 1;

        return rows;
    }

    public List<MonthlyStoreLeaderboard> BuildMonthlyStoreLeaderboards(
        IEnumerable<Booking> bookings,
        IEnumerable<Store> stores,
        IEnumerable<Mechanic> mechanics,
        DateTime financialYearStart,
        DateTime financialYearEnd,
        int topCount)
    {
        var result = new List<MonthlyStoreLeaderboard>();
        foreach (var month in SplitMonths(financialYearStart, financialYearEnd))
        {
            var monthRows = BuildStoreLeaderboard(
                bookings.Where(b => IsWithinRange(b.Start, month.StartDate, month.EndDate)),
                stores,
                mechanics)
                .Take(topCount)
                .ToList();

            result.Add(new MonthlyStoreLeaderboard(
                month.Label,
                month.StartDate,
                month.EndDate,
                monthRows));
        }

        return result;
    }

    public List<MonthlyMechanicLeaderboard> BuildMonthlyMechanicLeaderboards(
        IEnumerable<Booking> bookings,
        IEnumerable<Store> stores,
        IEnumerable<Mechanic> mechanics,
        DateTime financialYearStart,
        DateTime financialYearEnd,
        int topCount)
    {
        var result = new List<MonthlyMechanicLeaderboard>();
        foreach (var month in SplitMonths(financialYearStart, financialYearEnd))
        {
            var monthRows = BuildMechanicLeaderboard(
                bookings.Where(b => IsWithinRange(b.Start, month.StartDate, month.EndDate)),
                stores,
                mechanics)
                .Take(topCount)
                .ToList();

            result.Add(new MonthlyMechanicLeaderboard(
                month.Label,
                month.StartDate,
                month.EndDate,
                monthRows));
        }

        return result;
    }

    public List<Booking> FilterBookings(
        IEnumerable<Booking> bookings,
        UserAccessScope access,
        DateTime startDate,
        DateTime endDate)
    {
        var start = startDate.Date;
        var end = endDate.Date;

        return bookings
            .Where(b => access.CanAccessStore(b.StoreId))
            .Where(b =>
            {
                if (!access.HasMechanicRestrictions)
                    return true;

                return access.AllowedMechanicIds.Contains(b.MechanicId);
            })
            .Where(b => IsWithinRange(b.Start, start, end))
            .ToList();
    }

    private static bool IsWithinRange(DateTime candidate, DateTime start, DateTime end)
        => candidate.Date >= start.Date && candidate.Date <= end.Date;

    private static string ResolveStoreNameFromBookings(IEnumerable<Booking> bookings, Dictionary<int, Store> storeMap)
    {
        var firstStoreId = bookings.Select(b => b.StoreId).FirstOrDefault();
        return storeMap.TryGetValue(firstStoreId, out var store) ? store.Name : $"Store {firstStoreId}";
    }

    private static List<RevenuePoint> BuildRevenueTrend(IEnumerable<Booking> bookings, DateTime startDate, DateTime endDate)
    {
        var grouped = bookings
            .GroupBy(b => b.Start.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalPriceIncVat));

        var points = new List<RevenuePoint>();
        for (var day = startDate.Date; day <= endDate.Date; day = day.AddDays(1))
        {
            grouped.TryGetValue(day, out var revenue);
            points.Add(new RevenuePoint
            {
                Date = day,
                Revenue = revenue
            });
        }

        return points;
    }

    private static List<ServicePackageDescriptor> ResolveServicePackageCatalog(IEnumerable<Booking> bookings, IEnumerable<JobDefinition> jobs)
    {
        var configured = jobs
            .Where(j => j.Category.Equals("Service Packages", StringComparison.OrdinalIgnoreCase))
            .OrderBy(j => j.Name, StringComparer.OrdinalIgnoreCase)
            .Select(j => new ServicePackageDescriptor(j.Id, j.Name))
            .ToList();
        if (configured.Count > 0)
            return configured;

        return bookings
            .Select(b => (b.Title ?? "").Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
            .Select(t => new ServicePackageDescriptor(t, t))
            .ToList();
    }

    private List<PivotRow> BuildServicePivotByMechanic(
        IEnumerable<Booking> bookings,
        IEnumerable<Mechanic> mechanics,
        List<ServicePackageDescriptor> packages)
    {
        var mechanicMap = mechanics.ToDictionary(m => m.Id);
        return bookings
            .GroupBy(b => b.MechanicId)
            .OrderBy(g =>
            {
                var mechanic = mechanicMap.TryGetValue(g.Key, out var value) ? value : null;
                return mechanic?.Name ?? $"Mechanic {g.Key}";
            }, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var mechanic = mechanicMap.TryGetValue(group.Key, out var value) ? value : null;
                var rowName = mechanic?.Name ?? $"Mechanic {group.Key}";
                return BuildPivotRow(rowName, group, packages);
            })
            .ToList();
    }

    private List<PivotRow> BuildServicePivotByStore(
        IEnumerable<Booking> bookings,
        IEnumerable<Store> stores,
        List<ServicePackageDescriptor> packages)
    {
        var storeMap = stores.ToDictionary(s => s.Id);
        return bookings
            .GroupBy(b => b.StoreId)
            .OrderBy(g => storeMap.TryGetValue(g.Key, out var store) ? store.Name : $"Store {g.Key}", StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var rowName = storeMap.TryGetValue(group.Key, out var store) ? store.Name : $"Store {group.Key}";
                return BuildPivotRow(rowName, group, packages);
            })
            .ToList();
    }

    private PivotRow BuildPivotRow(string rowName, IEnumerable<Booking> bookings, List<ServicePackageDescriptor> packages)
    {
        var counts = packages.ToDictionary(p => p.Name, _ => 0, StringComparer.OrdinalIgnoreCase);
        var packageNamesById = packages.ToDictionary(p => p.Id, p => p.Name, StringComparer.OrdinalIgnoreCase);
        var fallbackNames = packages.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var booking in bookings)
        {
            var resolved = ResolveBookingServicePackages(booking, packageNamesById, fallbackNames);
            foreach (var packageName in resolved)
            {
                if (!counts.ContainsKey(packageName))
                    counts[packageName] = 0;
                counts[packageName] += 1;
            }
        }

        return new PivotRow
        {
            RowLabel = rowName,
            Counts = counts,
            Total = counts.Values.Sum()
        };
    }

    private static IEnumerable<string> ResolveBookingServicePackages(
        Booking booking,
        Dictionary<string, string> packageNamesById,
        HashSet<string> fallbackNames)
    {
        var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var jobId in booking.JobIds ?? Array.Empty<string>())
        {
            if (packageNamesById.TryGetValue(jobId, out var packageName))
                selected.Add(packageName);
        }

        if (!string.IsNullOrWhiteSpace(booking.JobId) && packageNamesById.TryGetValue(booking.JobId, out var directName))
            selected.Add(directName);

        if (selected.Count == 0 && !string.IsNullOrWhiteSpace(booking.Title))
        {
            var title = booking.Title.Trim();
            if (fallbackNames.Contains(title))
                selected.Add(title);
        }

        return selected;
    }

    private static decimal GetHoursWorked(Booking booking)
    {
        if (booking.TotalMinutes > 0)
            return booking.TotalMinutes / 60m;

        var diff = booking.End - booking.Start;
        if (diff.TotalMinutes > 0)
            return (decimal)diff.TotalHours;

        return 0m;
    }

    private static List<MonthRange> SplitMonths(DateTime start, DateTime end)
    {
        var monthRanges = new List<MonthRange>();
        var cursor = new DateTime(start.Year, start.Month, 1);
        while (cursor <= end)
        {
            var monthStart = cursor < start ? start.Date : cursor.Date;
            var monthEnd = cursor.AddMonths(1).AddDays(-1).Date;
            if (monthEnd > end.Date)
                monthEnd = end.Date;

            monthRanges.Add(new MonthRange(
                cursor.ToString("MMMM yyyy"),
                monthStart,
                monthEnd));

            cursor = cursor.AddMonths(1);
        }

        return monthRanges;
    }
}

public sealed record DashboardOverviewData(
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalRevenue,
    List<RevenuePoint> RevenueTrend,
    List<MechanicRevenueRow> TopMechanics,
    List<StoreRevenueRow> TopStores,
    List<string> ServicePackages,
    List<PivotRow> ServiceByMechanic,
    List<PivotRow> ServiceByStore);

public sealed class RevenuePoint
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
}

public sealed class MechanicRevenueRow
{
    public int Rank { get; set; }
    public int MechanicId { get; set; }
    public string Name { get; set; } = "-";
    public string Store { get; set; } = "";
    public decimal Revenue { get; set; }
    public decimal HoursWorked { get; set; }
    public decimal AvgPerHour
    {
        get
        {
            if (HoursWorked <= 0 || Revenue <= 0)
                return 0;

            var effectiveHours = HoursWorked < 1m ? 1m : HoursWorked;
            return Revenue / effectiveHours;
        }
    }
}

public sealed class StoreRevenueRow
{
    public int Rank { get; set; }
    public int StoreId { get; set; }
    public string Store { get; set; } = "";
    public decimal Revenue { get; set; }
    public decimal HoursWorked { get; set; }
}

public sealed class PivotRow
{
    public string RowLabel { get; set; } = "";
    public Dictionary<string, int> Counts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public int Total { get; set; }
}

public sealed record MonthlyStoreLeaderboard(string Label, DateTime StartDate, DateTime EndDate, List<StoreRevenueRow> Rows);
public sealed record MonthlyMechanicLeaderboard(string Label, DateTime StartDate, DateTime EndDate, List<MechanicRevenueRow> Rows);
public sealed record MonthRange(string Label, DateTime StartDate, DateTime EndDate);
public sealed record ServicePackageDescriptor(string Id, string Name);
