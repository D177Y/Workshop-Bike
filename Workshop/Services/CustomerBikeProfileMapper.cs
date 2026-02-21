using Workshop.Models;

namespace Workshop.Services;

public static class CustomerBikeProfileMapper
{
    public static CustomerBikeProfile? ParseBikeFromDetails(string bikeDetails)
    {
        var parsed = BikeDetailsService.ParseBikeFromDetails(bikeDetails);
        if (parsed is null)
            return null;

        return new CustomerBikeProfile
        {
            Id = Guid.NewGuid().ToString("N"),
            Make = parsed.Make,
            Model = parsed.Model,
            Size = parsed.Size,
            FrameNumber = parsed.FrameNumber,
            StockNumber = parsed.StockNumber
        };
    }

    public static string GetFingerprint(CustomerBikeProfile bike) =>
        string.Join("|", new[]
        {
            NormalizePart(bike.Make),
            NormalizePart(bike.Model),
            NormalizePart(bike.Size),
            NormalizePart(bike.FrameNumber),
            NormalizePart(bike.StockNumber)
        });

    private static string NormalizePart(string? value) =>
        (value ?? "").Trim().ToLowerInvariant();
}
