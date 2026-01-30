using Workshop.Models;

namespace Workshop.Services;

public sealed class SchedulingService
{
    private readonly WorkshopData _data;

    public SchedulingService(WorkshopData data)
    {
        _data = data;
    }

    public sealed record SlotResult(bool Found, int MechanicId, DateTime Start, DateTime End, string Reason);

    public SlotResult FindFirstSlot(int storeId, int minutes, DateTime earliest, int? mechanicId = null)
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
            if (!store.IsOpenOnDay(day))
                continue;

            var dayStart = day.Add(store.OpenFrom);
            var dayEnd = day.Add(store.OpenTo);

            var startFrom = cursor > dayStart ? cursor : dayStart;
            startFrom = RoundUpToFiveMinutes(startFrom);

            for (var t = startFrom; t.AddMinutes(minutes) <= dayEnd; t = t.AddMinutes(5))
            {
                var candidateStart = t;
                var candidateEnd = t.AddMinutes(minutes);

                foreach (var mech in mechanics)
                {
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

    public bool CanFitOnDay(int storeId, int minutes, DateTime day, int? mechanicId = null)
    {
        var store = _data.Stores.First(s => s.Id == storeId);

        // If the store is closed on this day, it cannot fit
        if (!store.IsOpenOnDay(day))
            return false;

        var dayStart = day.Date.Add(store.OpenFrom);
        var dayEnd = day.Date.Add(store.OpenTo);

        var slot = FindFirstSlot(storeId, minutes, dayStart, mechanicId);

        return slot.Found && slot.Start >= dayStart && slot.End <= dayEnd && slot.Start.Date == dayStart.Date;
    }

}
