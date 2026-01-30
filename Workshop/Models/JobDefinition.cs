namespace Workshop.Models;

public sealed class JobDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int DefaultMinutes { get; set; }
    public decimal BasePriceIncVat { get; set; }
}
