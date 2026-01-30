namespace Workshop.Models;

public sealed class Store
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public TimeSpan OpenFrom { get; set; }
    public TimeSpan OpenTo { get; set; }

    /// <summary>
    /// Days of the week the store is open (0 = Sunday, 6 = Saturday)
    /// </summary>
    public HashSet<DayOfWeek> DaysOpen { get; set; } = new();

    public bool IsOpenOnDay(DateTime date) => DaysOpen.Contains(date.DayOfWeek);
}
