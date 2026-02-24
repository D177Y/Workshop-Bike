using System.Collections.Concurrent;

namespace Workshop.Services;

public sealed class OperationalAlertService
{
    private static readonly TimeSpan MinimumAlertInterval = TimeSpan.FromMinutes(30);
    private readonly ConcurrentDictionary<string, DateTime> _lastSentByKey = new(StringComparer.OrdinalIgnoreCase);

    private readonly IConfiguration _configuration;
    private readonly ILogger<OperationalAlertService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public OperationalAlertService(
        IConfiguration configuration,
        ILogger<OperationalAlertService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task NotifyAsync(string alertKey, string subject, string message, Exception? exception = null, CancellationToken cancellationToken = default)
    {
        var settings = _configuration.GetSection("Operations");
        var enabled = settings.GetValue("EnableAlertEmails", false);
        var recipient = (settings["AlertEmail"] ?? "").Trim();

        if (!enabled || string.IsNullOrWhiteSpace(recipient))
            return;

        var nowUtc = DateTime.UtcNow;
        if (_lastSentByKey.TryGetValue(alertKey, out var lastSentUtc)
            && nowUtc - lastSentUtc < MinimumAlertInterval)
        {
            return;
        }

        var html = $"""
            <h2>Workshop operational alert</h2>
            <p><strong>Time (UTC):</strong> {nowUtc:O}</p>
            <p><strong>Alert key:</strong> {System.Net.WebUtility.HtmlEncode(alertKey)}</p>
            <p><strong>Message:</strong> {System.Net.WebUtility.HtmlEncode(message)}</p>
            <pre>{System.Net.WebUtility.HtmlEncode(exception?.ToString() ?? "")}</pre>
            """;
        var text = $"""
            Workshop operational alert
            Time (UTC): {nowUtc:O}
            Alert key: {alertKey}
            Message: {message}

            {exception}
            """;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
            await emailSender.SendAsync(recipient, subject, html, text);
            _lastSentByKey[alertKey] = nowUtc;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send operational alert email for key {AlertKey}.", alertKey);
        }
    }
}
