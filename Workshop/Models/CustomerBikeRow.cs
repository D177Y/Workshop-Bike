namespace Workshop.Models;

public sealed class CustomerBikeRow
{
    public string RowId { get; set; } = Guid.NewGuid().ToString("N");
    public string Make { get; set; } = "";
    public string Model { get; set; } = "";
    public string Size { get; set; } = "";
    public string FrameNumber { get; set; } = "";
    public string StockNumber { get; set; } = "";
}
