namespace Workshop.Services;

public static class BikeSelectionService
{
    public static string? ToggleSelection(string? currentSelectedBikeId, string bikeId, bool isChecked)
    {
        if (string.IsNullOrWhiteSpace(bikeId))
            return currentSelectedBikeId;

        if (isChecked)
            return bikeId;

        return string.Equals(currentSelectedBikeId, bikeId, StringComparison.OrdinalIgnoreCase)
            ? null
            : currentSelectedBikeId;
    }
}
