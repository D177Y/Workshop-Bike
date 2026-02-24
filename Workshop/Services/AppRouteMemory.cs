namespace Workshop.Services;

public static class AppRouteMemory
{
    public const string LastAppRouteStorageKey = "workshop.lastAppRoute";
    public const string DefaultAppRoute = "/dashboard/overview";
    public const string DefaultSuperAdminRoute = "/super/dashboard";

    private static readonly string[] ExcludedPrefixes =
    [
        "/",
        "/login",
        "/logout",
        "/signup",
        "/signup-success",
        "/get-started",
        "/pricing",
        "/features",
        "/how-it-works",
        "/forgot-password",
        "/reset-password",
        "/confirm-email",
        "/trial-access",
        "/billing",
        "/auth",
        "/not-found",
        "/error"
    ];

    public static string ResolveOpenWorkshopRoute(string? candidate, bool isSuperAdmin)
    {
        if (TryNormalizeForResume(candidate, isSuperAdmin, out var normalized))
            return normalized;

        return isSuperAdmin ? DefaultSuperAdminRoute : DefaultAppRoute;
    }

    public static bool TryNormalizeForResume(string? candidate, bool isSuperAdmin, out string normalized)
    {
        normalized = string.Empty;
        if (!TryNormalizeCandidate(candidate, out var path, out var querySuffix))
            return false;

        if (IsExcluded(path))
            return false;

        if (isSuperAdmin)
        {
            if (!path.StartsWith("/super", StringComparison.OrdinalIgnoreCase))
                return false;
        }
        else if (path.StartsWith("/super", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        normalized = path + querySuffix;
        return true;
    }

    private static bool TryNormalizeCandidate(string? candidate, out string path, out string querySuffix)
    {
        path = "/";
        querySuffix = string.Empty;

        if (string.IsNullOrWhiteSpace(candidate))
            return false;

        var trimmed = candidate.Trim();
        if (!Uri.IsWellFormedUriString(trimmed, UriKind.Relative))
            return false;

        if (!trimmed.StartsWith("/"))
            trimmed = "/" + trimmed;

        var hashIndex = trimmed.IndexOf('#');
        if (hashIndex >= 0)
            trimmed = trimmed[..hashIndex];

        var queryIndex = trimmed.IndexOf('?');
        if (queryIndex >= 0)
        {
            querySuffix = trimmed[queryIndex..];
            path = NormalizePath(trimmed[..queryIndex]);
            return true;
        }

        path = NormalizePath(trimmed);
        return true;
    }

    private static bool IsExcluded(string path)
    {
        foreach (var excluded in ExcludedPrefixes)
        {
            if (string.Equals(excluded, "/", StringComparison.Ordinal))
            {
                if (string.Equals(path, "/", StringComparison.Ordinal))
                    return true;

                continue;
            }

            if (string.Equals(path, excluded, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(excluded + "/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizePath(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "/";

        var normalized = value.StartsWith("/") ? value : "/" + value;
        return normalized.Length > 1 ? normalized.TrimEnd('/') : normalized;
    }
}
