namespace Workshop.Models;

public sealed class AddOnDefinition
{
    public string Id { get; set; } = "";
    public int TenantId { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
}
