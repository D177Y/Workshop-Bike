using Workshop.Models;

namespace Workshop.Services;

public static class BikeDetailsService
{
    public static string NewBikeRowId() => Guid.NewGuid().ToString("N");

    public static CustomerBikeRow CloneBike(CustomerBikeRow bike, bool keepRowId = false)
    {
        return new CustomerBikeRow
        {
            RowId = keepRowId
                ? (string.IsNullOrWhiteSpace(bike.RowId) ? NewBikeRowId() : bike.RowId)
                : NewBikeRowId(),
            Make = bike.Make,
            Model = bike.Model,
            Size = bike.Size,
            FrameNumber = bike.FrameNumber,
            StockNumber = bike.StockNumber
        };
    }

    public static string GetBikeFingerprint(CustomerBikeRow bike) =>
        $"{bike.Make}|{bike.Model}|{bike.Size}|{bike.FrameNumber}|{bike.StockNumber}"
            .Trim()
            .ToLowerInvariant();

    public static string BuildBikeDetailsText(CustomerBikeRow? bike)
    {
        if (bike is null)
            return "";

        var baseName = $"{bike.Make} {bike.Model}".Trim();
        var parts = new List<string> { baseName };

        if (!string.IsNullOrWhiteSpace(bike.Size))
            parts.Add($"Size {bike.Size.Trim()}");

        if (!string.IsNullOrWhiteSpace(bike.FrameNumber))
            parts.Add($"Frame {bike.FrameNumber.Trim()}");

        if (!string.IsNullOrWhiteSpace(bike.StockNumber))
            parts.Add($"Stock {bike.StockNumber.Trim()}");

        return string.Join(", ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    public static CustomerBikeRow? ParseBikeFromDetails(string bikeDetails)
    {
        if (string.IsNullOrWhiteSpace(bikeDetails))
            return null;

        var segments = bikeDetails
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0)
            return null;

        var firstSegment = segments[0];
        var words = firstSegment.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var make = words.Length > 0 ? words[0].Trim() : "";
        var model = words.Length > 1 ? string.Join(" ", words.Skip(1)).Trim() : "";
        var size = "";
        var frameNumber = "";
        var stockNumber = "";

        foreach (var segment in segments.Skip(1))
        {
            if (segment.StartsWith("Size ", StringComparison.OrdinalIgnoreCase))
            {
                size = segment["Size ".Length..].Trim();
                continue;
            }

            if (segment.StartsWith("Frame ", StringComparison.OrdinalIgnoreCase))
            {
                frameNumber = segment["Frame ".Length..].Trim();
                continue;
            }

            if (segment.StartsWith("Stock ", StringComparison.OrdinalIgnoreCase))
            {
                stockNumber = segment["Stock ".Length..].Trim();
            }
        }

        if (string.IsNullOrWhiteSpace(make))
            make = "Unknown";

        if (string.IsNullOrWhiteSpace(model))
            model = "Bike";

        return new CustomerBikeRow
        {
            RowId = NewBikeRowId(),
            Make = make,
            Model = model,
            Size = size,
            FrameNumber = frameNumber,
            StockNumber = stockNumber
        };
    }
}
