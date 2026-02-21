namespace Workshop.Services;

public static class StoreSchedulerColorService
{
    public static string SanitizeColor(string hex)
    {
        var value = (hex ?? "").Trim().TrimStart('#');
        return value.ToLowerInvariant();
    }

    public static string NormalizeColor(string value)
    {
        var v = (value ?? "").Trim();
        if (string.IsNullOrWhiteSpace(v))
            return "";

        return v.StartsWith("#", StringComparison.Ordinal) ? v : $"#{v}";
    }
}
