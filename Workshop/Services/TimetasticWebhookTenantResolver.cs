using Workshop.Models;

namespace Workshop.Services;

public static class TimetasticWebhookTenantResolver
{
    public static TimetasticWebhookTenantResolution ResolveBySecret(
        string secret,
        IReadOnlyCollection<IntegrationSettings> enabledSettings)
    {
        var matches = enabledSettings
            .Where(x => string.Equals((x.TimetasticWebhookSecret ?? "").Trim(), secret, StringComparison.Ordinal))
            .ToList();

        if (matches.Count == 0)
            return TimetasticWebhookTenantResolution.NotFound();

        if (matches.Count > 1)
            return TimetasticWebhookTenantResolution.Ambiguous();

        return TimetasticWebhookTenantResolution.Found(matches[0]);
    }
}

public sealed class TimetasticWebhookTenantResolution
{
    public IntegrationSettings? Settings { get; private init; }
    public bool IsNotFound { get; private init; }
    public bool IsAmbiguous { get; private init; }

    public static TimetasticWebhookTenantResolution Found(IntegrationSettings settings)
        => new() { Settings = settings };

    public static TimetasticWebhookTenantResolution NotFound()
        => new() { IsNotFound = true };

    public static TimetasticWebhookTenantResolution Ambiguous()
        => new() { IsAmbiguous = true };
}
