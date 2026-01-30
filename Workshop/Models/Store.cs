namespace Workshop.Models;

public sealed class Store
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public TimeSpan OpenFrom { get; set; }
    public TimeSpan OpenTo { get; set; }
}