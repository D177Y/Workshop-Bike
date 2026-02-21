namespace Workshop.Services;

public static class TimetasticApiConfiguration
{
    public static string NormalizeToken(string? token)
    {
        var normalized = (token ?? "").Trim();
        if (normalized.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            normalized = normalized["Bearer ".Length..].Trim();
        return normalized;
    }

    public static Uri? NormalizeBaseUri(string? value)
    {
        var text = (value ?? "").Trim();
        if (string.IsNullOrWhiteSpace(text))
            text = "https://app.timetastic.co.uk/api/";

        if (!text.EndsWith("/", StringComparison.Ordinal))
            text += "/";

        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri))
            return null;

        return uri;
    }
}
