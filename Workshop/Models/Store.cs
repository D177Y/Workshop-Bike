namespace Workshop.Models;

public sealed class Store
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string Email { get; set; } = "";
    public TimeSpan OpenFrom { get; set; }
    public TimeSpan OpenTo { get; set; }

    /// <summary>
    /// Days of the week the store is open (0 = Sunday, 6 = Saturday)
    /// </summary>
    public HashSet<DayOfWeek> DaysOpen { get; set; } = new();

    /// <summary>
    /// Per-day opening hours. If empty, OpenFrom/OpenTo + DaysOpen are used.
    /// </summary>
    public Dictionary<DayOfWeek, StoreDayHours> HoursByDay { get; set; } = new();

    public bool IsOpenOnDay(DateTime date)
    {
        if (HoursByDay.Count > 0)
            return HoursByDay.ContainsKey(date.DayOfWeek);

        return DaysOpen.Contains(date.DayOfWeek);
    }

    public bool TryGetHours(DateTime date, out TimeSpan openFrom, out TimeSpan openTo)
        => TryGetHours(date.DayOfWeek, out openFrom, out openTo);

    public bool TryGetHours(DayOfWeek day, out TimeSpan openFrom, out TimeSpan openTo)
    {
        if (HoursByDay.Count > 0)
        {
            if (HoursByDay.TryGetValue(day, out var hours))
            {
                openFrom = hours.OpenFrom;
                openTo = hours.OpenTo;
                return true;
            }

            openFrom = default;
            openTo = default;
            return false;
        }

        if (DaysOpen.Contains(day))
        {
            openFrom = OpenFrom;
            openTo = OpenTo;
            return true;
        }

        openFrom = default;
        openTo = default;
        return false;
    }
}

public sealed record StoreDayHours(TimeSpan OpenFrom, TimeSpan OpenTo);
