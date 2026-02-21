using Workshop.Models;

namespace Workshop.Services;

public sealed class SchedulingService
{
    private readonly WorkshopData _data;
    private readonly JobCatalogService _catalog;

    public SchedulingService(WorkshopData data, JobCatalogService catalog)
    {
        _data = data;
        _catalog = catalog;
    }

    public sealed record SlotResult(bool Found, int MechanicId, DateTime Start, DateTime End, string Reason);

    public SlotResult FindFirstSlot(int storeId, int minutes, DateTime earliest, int? mechanicId = null, IReadOnlyCollection<string>? jobIds = null)
    {
        var store = _data.Stores.First(s => s.Id == storeId);

        var mechanics = _data.Mechanics
            .Where(m => m.StoreId == storeId)
            .ToList();

        if (mechanicId.HasValue)
            mechanics = mechanics.Where(m => m.Id == mechanicId.Value).ToList();

        if (mechanics.Count == 0)
            return new SlotResult(false, 0, default, default, "No mechanics available for that selection");

        var cursor = earliest;

        for (var dayOffset = 0; dayOffset < 60; dayOffset++)
        {
            var day = cursor.Date.AddDays(dayOffset);

            // Skip closed days
            if (!store.TryGetHours(day, out var storeOpenFrom, out var storeOpenTo))
                continue;

            var dayStart = day.Add(storeOpenFrom);
            var dayEnd = day.Add(storeOpenTo);

            var startFrom = cursor > dayStart ? cursor : dayStart;
            startFrom = RoundUpToFiveMinutes(startFrom);

            for (var t = startFrom; t.AddMinutes(minutes) <= dayEnd; t = t.AddMinutes(5))
            {
                var candidateStart = t;
                var candidateEnd = t.AddMinutes(minutes);

                foreach (var mech in mechanics)
                {
                    if (!CanMechanicDoJobs(mech, jobIds))
                        continue;

                    if (!TryGetMechanicWindow(store, mech, day, out var mechStart, out var mechEnd))
                        continue;

                    if (candidateStart < mechStart || candidateEnd > mechEnd)
                        continue;

                    if (!FitsWithoutOverlap(storeId, mech.Id, candidateStart, candidateEnd))
                        continue;

                    if (!FitsDailyCapacity(storeId, mech, candidateStart, candidateEnd))
                        continue;

                    return new SlotResult(true, mech.Id, candidateStart, candidateEnd, "");
                }
            }
        }

        return new SlotResult(false, 0, default, default, "No slot found in the next 60 days");
    }


    public bool WouldExceedDailyLimit(int storeId, int mechanicId, DateTime start, DateTime end, int? bookingIdToIgnore = null)
    {
        var mech = _data.Mechanics.First(m => m.Id == mechanicId && m.StoreId == storeId);
        var day = start.Date;

        var bookedHours = _data.Bookings
            .Where(b => b.StoreId == storeId && b.MechanicId == mechanicId && b.Start.Date == day)
            .Where(b => bookingIdToIgnore is null || b.Id != bookingIdToIgnore.Value)
            .Sum(b => (b.End - b.Start).TotalHours);

        var newHours = (end - start).TotalHours;
        return bookedHours + newHours > mech.MaxBookableHoursPerDay;
    }

    private bool FitsDailyCapacity(int storeId, Mechanic mech, DateTime start, DateTime end, int? bookingIdToIgnore = null)
        => !WouldExceedDailyLimit(storeId, mech.Id, start, end, bookingIdToIgnore);

    private bool FitsWithoutOverlap(int storeId, int mechanicId, DateTime start, DateTime end, int? bookingIdToIgnore = null)
    {
        foreach (var b in _data.Bookings.Where(x => x.StoreId == storeId && x.MechanicId == mechanicId))
        {
            if (bookingIdToIgnore.HasValue && b.Id == bookingIdToIgnore.Value) continue;
            if (Overlaps(start, end, b.Start, b.End)) return false;
        }

        foreach (var timeOff in _data.MechanicTimeOffEntries.Where(x => x.StoreId == storeId && x.MechanicId == mechanicId))
        {
            if (Overlaps(start, end, timeOff.Start, timeOff.End))
                return false;
        }

        return true;
    }

    private static bool Overlaps(DateTime aStart, DateTime aEnd, DateTime bStart, DateTime bEnd)
        => aStart < bEnd && bStart < aEnd;

    private static DateTime RoundUpToFiveMinutes(DateTime dt)
    {
        var minutes = dt.Minute;
        var mod = minutes % 5;
        if (mod == 0) return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0);
        var add = 5 - mod;
        var rounded = dt.AddMinutes(add);
        return new DateTime(rounded.Year, rounded.Month, rounded.Day, rounded.Hour, rounded.Minute, 0);
    }

    public bool CanFitOnDay(int storeId, int minutes, DateTime day, int? mechanicId = null, IReadOnlyCollection<string>? jobIds = null)
    {
        var store = _data.Stores.First(s => s.Id == storeId);

        // If the store is closed on this day, it cannot fit
        if (!store.TryGetHours(day, out var openFrom, out var openTo))
            return false;

        var dayStart = day.Date.Add(openFrom);
        var dayEnd = day.Date.Add(openTo);

        var slot = FindFirstSlot(storeId, minutes, dayStart, mechanicId, jobIds);

        return slot.Found && slot.Start >= dayStart && slot.End <= dayEnd && slot.Start.Date == dayStart.Date;
    }

    private bool CanMechanicDoJobs(Mechanic mech, IReadOnlyCollection<string>? jobIds)
    {
        if (jobIds == null || jobIds.Count == 0)
            return true;

        foreach (var jobId in jobIds)
        {
            var job = _catalog.Jobs.FirstOrDefault(j => j.Id == jobId);
            if (job == null)
                return false;

            if (!_catalog.CanMechanicPerformJob(mech, job))
                return false;
        }

        return true;
    }

    private static bool TryGetMechanicWindow(Store store, Mechanic mech, DateTime day, out DateTime start, out DateTime end)
    {
        if (!store.TryGetHours(day, out var storeOpenFrom, out var storeOpenTo))
        {
            start = default;
            end = default;
            return false;
        }

        if (mech.HoursByDay.Count > 0)
        {
            if (!mech.HoursByDay.TryGetValue(day.DayOfWeek, out var hours))
            {
                start = default;
                end = default;
                return false;
            }

            var mechStart = day.Date.Add(hours.OpenFrom);
            var mechEnd = day.Date.Add(hours.OpenTo);
            var storeStart = day.Date.Add(storeOpenFrom);
            var storeEnd = day.Date.Add(storeOpenTo);

            start = mechStart > storeStart ? mechStart : storeStart;
            end = mechEnd < storeEnd ? mechEnd : storeEnd;
            return start < end;
        }

        if (mech.DaysWorking.Contains(day.DayOfWeek))
        {
            start = day.Date.Add(storeOpenFrom);
            end = day.Date.Add(storeOpenTo);
            return start < end;
        }

        start = default;
        end = default;
        return false;
    }

}
