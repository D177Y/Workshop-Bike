namespace Workshop.Models;

public sealed class ServicePackageChecklistItemDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public int SortOrder { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string LinkedServiceJobId { get; set; } = "";
}
