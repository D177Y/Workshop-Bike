namespace Workshop.Services;

public sealed class StripeSubscriptionReconciliationBackgroundService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(6);

    private readonly StripeSubscriptionSyncService _subscriptionSync;
    private readonly OperationalAlertService _alerts;
    private readonly ILogger<StripeSubscriptionReconciliationBackgroundService> _logger;

    public StripeSubscriptionReconciliationBackgroundService(
        StripeSubscriptionSyncService subscriptionSync,
        OperationalAlertService alerts,
        ILogger<StripeSubscriptionReconciliationBackgroundService> logger)
    {
        _subscriptionSync = subscriptionSync;
        _alerts = alerts;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var summary = await _subscriptionSync.ReconcileAsync(stoppingToken);
                if (summary.CheckedTenants > 0)
                {
                    _logger.LogInformation(
                        "Stripe reconciliation checked {Checked} tenants; updated {Updated}; failed {Failed}.",
                        summary.CheckedTenants,
                        summary.UpdatedTenants,
                        summary.FailedTenants);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Stripe reconciliation worker iteration failed.");
                await _alerts.NotifyAsync(
                    "stripe-reconcile",
                    "Workshop alert: Stripe reconciliation failed",
                    "The Stripe subscription reconciliation background worker failed.",
                    ex,
                    stoppingToken);
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
