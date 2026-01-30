namespace Workshop.Models;

public sealed class Mechanic
{
    public int Id { get; set; }
    public int StoreId { get; set; }
    public string Name { get; set; } = "";
    public double MaxBookableHoursPerDay { get; set; } = 6.0;
}
