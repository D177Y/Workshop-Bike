using System.Text;

namespace Workshop.Services;

internal static class ServicePackageReductionKeyHelper
{
    private const string GlobalPackagePrefix = "GLOBAL_PACKAGE::";

    internal static string NormalizePackageName(string? packageName)
        => (packageName ?? "").Trim();

    internal static string BuildGlobalPackageReductionKey(string? packageName)
    {
        var normalized = NormalizePackageName(packageName);
        return string.IsNullOrWhiteSpace(normalized)
            ? ""
            : $"{GlobalPackagePrefix}{normalized.ToUpperInvariant()}";
    }

    internal static IEnumerable<string> BuildGlobalPackageReductionKeyCandidates(string? packageName)
    {
        var normalized = NormalizePackageName(packageName);
        if (string.IsNullOrWhiteSpace(normalized))
            return Array.Empty<string>();

        var candidates = new List<string>
        {
            BuildGlobalPackageReductionKey(normalized),
            normalized
        };

        var legacyForFullName = BuildLegacyServicePackageJobId(normalized);
        if (!string.IsNullOrWhiteSpace(legacyForFullName))
            candidates.Add(legacyForFullName);

        var withoutSuffix = TrimTrailingServiceWord(normalized);
        if (!withoutSuffix.Equals(normalized, StringComparison.OrdinalIgnoreCase))
        {
            var legacyForTrimmedName = BuildLegacyServicePackageJobId(withoutSuffix);
            if (!string.IsNullOrWhiteSpace(legacyForTrimmedName))
                candidates.Add(legacyForTrimmedName);
        }

        return candidates.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static string TrimTrailingServiceWord(string value)
    {
        var normalized = (value ?? "").Trim();
        return normalized.EndsWith(" service", StringComparison.OrdinalIgnoreCase)
            ? normalized[..^8].Trim()
            : normalized;
    }

    private static string BuildLegacyServicePackageJobId(string packageName)
    {
        var token = SlugifyToken(packageName);
        return string.IsNullOrWhiteSpace(token) ? "" : $"SVC_{token}";
    }

    private static string SlugifyToken(string? value)
    {
        var builder = new StringBuilder();
        var lastWasUnderscore = false;
        foreach (var ch in (value ?? "").Trim().ToUpperInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
                lastWasUnderscore = false;
            }
            else if (!lastWasUnderscore)
            {
                builder.Append('_');
                lastWasUnderscore = true;
            }
        }

        return builder.ToString().Trim('_');
    }
}
