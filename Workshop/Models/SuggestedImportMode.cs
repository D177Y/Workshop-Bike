namespace Workshop.Models;

public enum SuggestedImportMode
{
    CategoriesOnly = 0,
    ServicesOnly = 1,
    ServicesAndTimes = 2,
    ServicePrices = 3,
    FullSetup = 4
}

public sealed class SuggestedImportResult
{
    public int CategoriesAdded { get; set; }
    public int CategoriesUpdated { get; set; }
    public int ServicesAdded { get; set; }
    public int ServicesUpdated { get; set; }
}
