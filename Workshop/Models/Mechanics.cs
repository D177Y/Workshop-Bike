namespace Workshop.Models;

public sealed class Mechanic
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int StoreId { get; set; }
    public string Name { get; set; } = "";
    public string MobileNumber { get; set; } = "";
    public string Email { get; set; } = "";
    public double MaxBookableHoursPerDay { get; set; } = 6.0;

    public HashSet<DayOfWeek> DaysWorking { get; set; } = new();
    public Dictionary<DayOfWeek, StoreDayHours> HoursByDay { get; set; } = new();

    public string SkillLevel { get; set; } = "Advanced";
    public HashSet<string> CustomAllowedJobIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);

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

        if (DaysWorking.Contains(day))
        {
            openFrom = default;
            openTo = default;
            return true;
        }

        openFrom = default;
        openTo = default;
        return false;
    }
}
