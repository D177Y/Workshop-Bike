namespace Workshop.Models;

public sealed class GlobalServiceCategory
{
    public int Id { get; set; }
    public string Category1 { get; set; } = "";
    public string Category2 { get; set; } = "";
    public string ColorHex { get; set; } = "#94a3b8";
    public int SortOrder { get; set; }
}
