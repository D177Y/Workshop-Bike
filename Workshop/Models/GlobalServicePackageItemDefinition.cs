namespace Workshop.Models;

public sealed class GlobalServicePackageItemDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public int SortOrder { get; set; }
    public GlobalServicePackageItemType ItemType { get; set; } = GlobalServicePackageItemType.Manual;
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int? LinkedGlobalServiceTemplateId { get; set; }
    public int? IncludedGlobalServicePackageTemplateId { get; set; }
}
