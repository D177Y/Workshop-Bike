using Syncfusion.Blazor.Calendars;

namespace Workshop.Components.Pages;

public partial class CreateBooking
{
    private void UpdateEarliestAvailableDay()
    {
        if (CalculatedMinutes <= 0)
        {
            EarliestAvailableDay = null;
            return;
        }

        var now = DateTime.Now;
        var first = FindFirstBookableDayFrom(now);
        if (first.HasValue)
        {
            EarliestAvailableDay = first.Value;
            Message = "";
            return;
        }

        EarliestAvailableDay = null;
        Message = "No availability found in the next 60 days. Try another store, mechanic, or job.";
    }

    private void RefreshMonthAvailability()
    {
        var baseDay = (EarliestDay ?? EarliestAvailableDay ?? DateTime.Today).Date;
        RefreshMonthAvailability(baseDay);
    }

    private void RefreshMonthAvailability(DateTime baseDay)
    {
        UnavailableDays.Clear();
        ClosedDays.Clear();

        if (CalculatedMinutes <= 0) return;

        var store = GetSelectedStore();
        if (store is null) return;
        var today = DateTime.Today;

        var monthStart = new DateTime(baseDay.Year, baseDay.Month, 1);
        var monthEnd = monthStart.AddMonths(1);

        int? mechanicFilter = _selectedMechanicId == 0 ? null : _selectedMechanicId;

        for (var d = monthStart; d < monthEnd; d = d.AddDays(1))
        {
            if (!store.IsOpenOnDay(d))
            {
                ClosedDays.Add(d.Date);
                continue;
            }

            var canFit = d.Date == today
                ? HasBookableSlotOnDay(d)
                : Scheduler.CanFitOnDay(SelectedStoreId, CalculatedMinutes, d, mechanicFilter, GetAllSelectedJobIds());
            if (!canFit)
                UnavailableDays.Add(d.Date);
        }

        _availabilityMonth = baseDay.Month;
        _availabilityYear = baseDay.Year;
    }

    private void EnsureSelectedDayIsValid()
    {
        if (!EarliestDay.HasValue)
        {
            if (EarliestAvailableDay.HasValue)
                EarliestDay = EarliestAvailableDay.Value;
            return;
        }

        if (!EarliestAvailableDay.HasValue)
        {
            EarliestDay = null;
            return;
        }

        var day = EarliestDay.Value.Date;
        var today = DateTime.Today;

        if (day < today)
        {
            EarliestDay = EarliestAvailableDay.Value;
            return;
        }

        if (day < EarliestAvailableDay.Value.Date)
        {
            EarliestDay = EarliestAvailableDay.Value;
            return;
        }

        if (ClosedDays.Contains(day))
        {
            var next = FindFirstBookableDayFrom(day.Date.AddDays(1));
            if (next.HasValue)
            {
                EarliestDay = next.Value;
                Message = "";
                RefreshMonthAvailability();
                return;
            }

            EarliestDay = null;
            Message = "Store is closed on this day. Please select another day.";
            return;
        }

        if (!HasBookableSlotOnDay(day))
        {
            var next = FindNextAvailableDay(day);
            if (next.HasValue)
            {
                EarliestDay = next.Value;
                Message = "";
                RefreshMonthAvailability();
            }
            else
            {
                EarliestDay = null;
                Message = "No availability found in the next 60 days. Try a different job, store, or mechanic.";
            }
            return;
        }

        if (UnavailableDays.Contains(day))
        {
            var next = FindNextAvailableDay(day);
            if (next.HasValue)
            {
                EarliestDay = next.Value;
                Message = "";
                RefreshMonthAvailability();
            }
            else
            {
                EarliestDay = null;
                Message = "No availability found in the current range. Try a different job, store, or mechanic.";
            }
        }
    }

    private DateTime? FindEarlierAvailableDay(DateTime fromDay)
    {
        var today = DateTime.Today;
        var start = fromDay.Date.AddDays(-1);
        var store = GetSelectedStore();
        if (store is null) return null;

        if (start < today) return null;

        int? mechanicFilter = _selectedMechanicId == 0 ? null : _selectedMechanicId;

        for (var d = start; d >= today; d = d.AddDays(-1))
        {
            if (!store.IsOpenOnDay(d))
                continue;

            var canFit = d.Date == today
                ? HasBookableSlotOnDay(d)
                : Scheduler.CanFitOnDay(SelectedStoreId, CalculatedMinutes, d, mechanicFilter, GetAllSelectedJobIds());
            if (canFit)
                return d.Date;
        }

        return null;
    }

    private DateTime? FindNextAvailableDay(DateTime fromDay)
    {
        var start = fromDay.Date;
        var end = start.AddDays(90);
        var store = GetSelectedStore();
        if (store is null) return null;

        int? mechanicFilter = _selectedMechanicId == 0 ? null : _selectedMechanicId;

        var today = DateTime.Today;
        for (var d = start; d <= end; d = d.AddDays(1))
        {
            if (!store.IsOpenOnDay(d))
                continue;

            var currentBase = new DateTime(_availabilityYear == 0 ? d.Year : _availabilityYear, _availabilityMonth == 0 ? d.Month : _availabilityMonth, 1);
            if (d.Month != currentBase.Month || d.Year != currentBase.Year)
            {
                RefreshMonthAvailability(d.Date);
            }

            var canFit = d.Date == today
                ? HasBookableSlotOnDay(d)
                : Scheduler.CanFitOnDay(SelectedStoreId, CalculatedMinutes, d, mechanicFilter, GetAllSelectedJobIds());
            if (canFit)
                return d.Date;
        }

        return null;
    }

    private void OnCalendarRenderDayCell(RenderDayCellEventArgs args)
    {
        var day = args.Date.Date;

        if (day.Month != _availabilityMonth || day.Year != _availabilityYear)
            RefreshMonthAvailability(day);

        if (ClosedDays.Contains(day))
        {
            args.IsDisabled = true;
            args.CellData.ClassList = (args.CellData.ClassList ?? "") + " day-closed";
        }
        else if (UnavailableDays.Contains(day))
        {
            args.IsDisabled = true;
            args.CellData.ClassList = (args.CellData.ClassList ?? "") + " day-unavailable";
        }
    }

    private void OnEarliestDayChanged(ChangedEventArgs<DateTime?> args)
    {
        var value = args.Value;

        if (!value.HasValue)
        {
            EarliestDay = null;
            return;
        }

        var picked = value.Value.Date;
        var today = DateTime.Today;
        if (picked < today)
        {
            Message = "Pick a day that is today or later.";
            return;
        }

        if (ClosedDays.Contains(picked))
        {
            Message = "Store is closed on that day. Please select another day.";
            return;
        }

        if (!HasBookableSlotOnDay(picked))
        {
            Message = "No availability on that day for the selected job duration. Pick another day.";
            return;
        }

        Message = "";
        EarliestDay = picked;

        RefreshMonthAvailability();
    }

    private bool HasBookableSlotOnDay(DateTime day)
    {
        if (CalculatedMinutes <= 0) return false;

        var store = GetSelectedStore();
        if (store is null) return false;
        if (!store.TryGetHours(day, out var openFrom, out var openTo))
            return false;

        var dayStart = day.Date.Add(openFrom);
        var dayEnd = day.Date.Add(openTo);
        var earliest = dayStart;

        if (day.Date == DateTime.Today)
        {
            var now = DateTime.Now;
            if (now > earliest)
                earliest = now;
        }

        int? mechanicFilter = _selectedMechanicId == 0 ? null : _selectedMechanicId;
        var slot = Scheduler.FindFirstSlot(SelectedStoreId, CalculatedMinutes, earliest, mechanicFilter, GetAllSelectedJobIds());

        return slot.Found && slot.Start.Date == day.Date && slot.Start >= dayStart && slot.End <= dayEnd;
    }

    private DateTime? FindFirstBookableDayFrom(DateTime from)
    {
        if (CalculatedMinutes <= 0) return null;

        int? mechanicFilter = _selectedMechanicId == 0 ? null : _selectedMechanicId;
        var slot = Scheduler.FindFirstSlot(SelectedStoreId, CalculatedMinutes, from, mechanicFilter, GetAllSelectedJobIds());
        if (!slot.Found) return null;

        return slot.Start.Date;
    }
}
