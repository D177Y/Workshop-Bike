using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using Workshop.Data;
using Workshop.Models;
using PlanTier = Workshop.Models.PlanTier;

namespace Workshop.Services;

public sealed class StripeSubscriptionSyncService
{
    private readonly IDbContextFactory<WorkshopDbContext> _dbFactory;
    private readonly BillingService _billing;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeSubscriptionSyncService> _logger;

    public StripeSubscriptionSyncService(
        IDbContextFactory<WorkshopDbContext> dbFactory,
        BillingService billing,
        IConfiguration configuration,
        ILogger<StripeSubscriptionSyncService> logger)
    {
        _dbFactory = dbFactory;
        _billing = billing;
        _configuration = configuration;
        _logger = logger;
    }

    public bool IsConfigured()
        => !string.IsNullOrWhiteSpace(_configuration["Stripe:SecretKey"]);

    public async Task HandleWebhookAsync(Event stripeEvent, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
            return;

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        switch (stripeEvent.Type)
        {
            case Events.CheckoutSessionCompleted:
            {
                if (stripeEvent.Data.Object is Session session)
                    await HandleCheckoutCompletedAsync(db, session, cancellationToken);
                break;
            }
            case Events.CustomerSubscriptionCreated:
            case Events.CustomerSubscriptionUpdated:
            case Events.CustomerSubscriptionDeleted:
            {
                if (stripeEvent.Data.Object is Subscription subscription)
                    await HandleSubscriptionEventAsync(db, subscription, stripeEvent.Type, cancellationToken);
                break;
            }
            case Events.InvoicePaymentFailed:
            case Events.InvoicePaymentSucceeded:
            {
                if (stripeEvent.Data.Object is Invoice invoice)
                    await HandleInvoiceEventAsync(db, invoice, stripeEvent.Type, cancellationToken);
                break;
            }
            default:
                return;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<StripeReconciliationSummary> ReconcileAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
            return new StripeReconciliationSummary(0, 0, 0, 0);

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var tenants = await db.Tenants
            .Where(t =>
                t.HasActivatedSubscription
                || !string.IsNullOrWhiteSpace(t.StripeSubscriptionId)
                || !string.IsNullOrWhiteSpace(t.StripeCustomerId))
            .OrderBy(t => t.Id)
            .ToListAsync(cancellationToken);

        var updated = 0;
        var failed = 0;
        var checkedCount = 0;
        foreach (var tenant in tenants)
        {
            checkedCount++;
            try
            {
                var changed = await ReconcileTenantAsync(tenant, cancellationToken);
                if (changed)
                    updated++;
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogWarning(ex, "Stripe reconciliation failed for tenant {TenantId}.", tenant.Id);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return new StripeReconciliationSummary(tenants.Count, checkedCount, updated, failed);
    }

    private async Task<bool> ReconcileTenantAsync(Tenant tenant, CancellationToken cancellationToken)
    {
        var subscription = await FetchSubscriptionForTenantAsync(tenant, cancellationToken);
        if (subscription is null)
        {
            if (tenant.HasActivatedSubscription)
            {
                var changed = false;
                var normalized = StripeBillingPolicy.NormalizeStatus(tenant.StripeSubscriptionStatus);
                if (normalized is "" or "active" or "trialing" or "past_due")
                {
                    tenant.StripeSubscriptionStatus = "canceled";
                    changed = true;
                }

                if (tenant.IsActive)
                {
                    tenant.IsActive = false;
                    changed = true;
                }

                if (changed)
                {
                    tenant.StripeSubscriptionUpdatedAtUtc = DateTime.UtcNow;
                    return true;
                }
            }

            return false;
        }

        var plan = ResolvePlanFromSubscription(subscription);
        var status = StripeBillingPolicy.NormalizeStatus(subscription.Status);
        var currentPeriodEndUtc = subscription.CurrentPeriodEnd;
        return ApplySubscriptionState(
            tenant,
            subscription.CustomerId,
            subscription.Id,
            status,
            plan,
            currentPeriodEndUtc);
    }

    private async Task HandleCheckoutCompletedAsync(WorkshopDbContext db, Session session, CancellationToken cancellationToken)
    {
        var metadata = session.Metadata ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var tenantId = TryParseInt(metadata.TryGetValue("tenant_id", out var tenantRaw) ? tenantRaw : null);
        var tenant = await ResolveTenantAsync(db, tenantId, session.SubscriptionId, session.CustomerId, cancellationToken);
        if (tenant is null)
        {
            _logger.LogWarning(
                "Stripe checkout.session.completed could not resolve tenant. SubscriptionId={SubscriptionId}, CustomerId={CustomerId}.",
                session.SubscriptionId,
                session.CustomerId);
            return;
        }

        PlanTier? plan = TryParsePlan(metadata.TryGetValue("plan", out var planRaw) ? planRaw : null);
        var status = "active";
        DateTime? currentPeriodEndUtc = null;

        if (!string.IsNullOrWhiteSpace(session.SubscriptionId))
        {
            try
            {
                var subscriptionService = new SubscriptionService();
                var subscription = await subscriptionService.GetAsync(session.SubscriptionId, cancellationToken: cancellationToken);
                status = StripeBillingPolicy.NormalizeStatus(subscription.Status);
                currentPeriodEndUtc = subscription.CurrentPeriodEnd;
                plan ??= ResolvePlanFromSubscription(subscription);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to fetch Stripe subscription {SubscriptionId} after checkout completion.", session.SubscriptionId);
            }
        }

        ApplySubscriptionState(
            tenant,
            session.CustomerId,
            session.SubscriptionId,
            status,
            plan,
            currentPeriodEndUtc);
    }

    private async Task HandleSubscriptionEventAsync(
        WorkshopDbContext db,
        Subscription subscription,
        string eventType,
        CancellationToken cancellationToken)
    {
        var metadataTenantId = TryParseInt(GetMetadataValue(subscription.Metadata, "tenant_id"));
        var tenant = await ResolveTenantAsync(db, metadataTenantId, subscription.Id, subscription.CustomerId, cancellationToken);
        if (tenant is null)
        {
            _logger.LogWarning(
                "Stripe subscription event {EventType} could not resolve tenant. SubscriptionId={SubscriptionId}, CustomerId={CustomerId}.",
                eventType,
                subscription.Id,
                subscription.CustomerId);
            return;
        }

        var plan = TryParsePlan(GetMetadataValue(subscription.Metadata, "plan")) ?? ResolvePlanFromSubscription(subscription);
        var status = eventType == Events.CustomerSubscriptionDeleted
            ? "canceled"
            : StripeBillingPolicy.NormalizeStatus(subscription.Status);
        var currentPeriodEndUtc = subscription.CurrentPeriodEnd;

        ApplySubscriptionState(
            tenant,
            subscription.CustomerId,
            subscription.Id,
            status,
            plan,
            currentPeriodEndUtc);
    }

    private async Task HandleInvoiceEventAsync(
        WorkshopDbContext db,
        Invoice invoice,
        string eventType,
        CancellationToken cancellationToken)
    {
        var tenant = await ResolveTenantAsync(db, null, invoice.SubscriptionId, invoice.CustomerId, cancellationToken);
        if (tenant is null)
            return;

        Subscription? subscription = null;
        if (!string.IsNullOrWhiteSpace(invoice.SubscriptionId))
        {
            try
            {
                var subscriptionService = new SubscriptionService();
                subscription = await subscriptionService.GetAsync(invoice.SubscriptionId, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to fetch Stripe subscription {SubscriptionId} during invoice event handling.", invoice.SubscriptionId);
            }
        }

        var plan = subscription is null
            ? null
            : ResolvePlanFromSubscription(subscription);
        var status = subscription is null
            ? (eventType == Events.InvoicePaymentFailed ? "past_due" : "active")
            : StripeBillingPolicy.NormalizeStatus(subscription.Status);
        var currentPeriodEndUtc = subscription?.CurrentPeriodEnd;

        ApplySubscriptionState(
            tenant,
            invoice.CustomerId,
            invoice.SubscriptionId,
            status,
            plan,
            currentPeriodEndUtc);
    }

    private static PlanTier? TryParsePlan(string? rawPlan)
    {
        if (PlanCatalog.TryParseKey(rawPlan, out var tier))
            return tier;

        return Enum.TryParse<PlanTier>((rawPlan ?? "").Trim(), true, out var parsed)
            ? parsed
            : null;
    }

    private PlanTier? ResolvePlanFromSubscription(Subscription subscription)
    {
        var priceId = subscription.Items?.Data?.FirstOrDefault()?.Price?.Id;
        return _billing.ResolvePlanFromPriceId(priceId);
    }

    private static int? TryParseInt(string? raw)
        => int.TryParse((raw ?? "").Trim(), out var parsed)
            ? parsed
            : null;

    private static string? GetMetadataValue(IDictionary<string, string>? metadata, string key)
    {
        if (metadata is null)
            return null;

        return metadata.TryGetValue(key, out var value)
            ? value
            : null;
    }

    private static bool ApplySubscriptionState(
        Tenant tenant,
        string? customerId,
        string? subscriptionId,
        string status,
        PlanTier? plan,
        DateTime? currentPeriodEndUtc)
    {
        var changed = false;

        var normalizedCustomerId = (customerId ?? "").Trim();
        var normalizedSubscriptionId = (subscriptionId ?? "").Trim();
        var normalizedStatus = StripeBillingPolicy.NormalizeStatus(status);

        if (!string.IsNullOrWhiteSpace(normalizedCustomerId) && !string.Equals(tenant.StripeCustomerId, normalizedCustomerId, StringComparison.Ordinal))
        {
            tenant.StripeCustomerId = normalizedCustomerId;
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(normalizedSubscriptionId) && !string.Equals(tenant.StripeSubscriptionId, normalizedSubscriptionId, StringComparison.Ordinal))
        {
            tenant.StripeSubscriptionId = normalizedSubscriptionId;
            changed = true;
        }

        if (!string.Equals(tenant.StripeSubscriptionStatus, normalizedStatus, StringComparison.Ordinal))
        {
            tenant.StripeSubscriptionStatus = normalizedStatus;
            changed = true;
        }

        if (tenant.StripeCurrentPeriodEndUtc != currentPeriodEndUtc)
        {
            tenant.StripeCurrentPeriodEndUtc = currentPeriodEndUtc;
            changed = true;
        }

        if (plan.HasValue && tenant.Plan != plan.Value)
        {
            tenant.Plan = plan.Value;
            changed = true;
        }

        if (plan.HasValue)
        {
            var expectedLimit = PlanCatalog.GetMechanicLimit(plan.Value);
            if (tenant.MaxMechanics != expectedLimit)
            {
                tenant.MaxMechanics = expectedLimit;
                changed = true;
            }
        }

        if (!tenant.HasActivatedSubscription && !string.IsNullOrWhiteSpace(tenant.StripeSubscriptionId))
        {
            tenant.HasActivatedSubscription = true;
            changed = true;
        }

        var hasBillableAccess = StripeBillingPolicy.IsPaidStatus(normalizedStatus);
        if (tenant.IsActive != hasBillableAccess)
        {
            tenant.IsActive = hasBillableAccess;
            changed = true;
        }

        if (changed)
            tenant.StripeSubscriptionUpdatedAtUtc = DateTime.UtcNow;

        return changed;
    }

    private static async Task<Tenant?> ResolveTenantAsync(
        WorkshopDbContext db,
        int? tenantId,
        string? subscriptionId,
        string? customerId,
        CancellationToken cancellationToken)
    {
        if (tenantId.HasValue && tenantId.Value > 0)
        {
            var byId = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId.Value, cancellationToken);
            if (byId is not null)
                return byId;
        }

        var normalizedSubscriptionId = (subscriptionId ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(normalizedSubscriptionId))
        {
            var bySubscription = await db.Tenants.FirstOrDefaultAsync(
                t => t.StripeSubscriptionId == normalizedSubscriptionId,
                cancellationToken);
            if (bySubscription is not null)
                return bySubscription;
        }

        var normalizedCustomerId = (customerId ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(normalizedCustomerId))
        {
            return await db.Tenants.FirstOrDefaultAsync(
                t => t.StripeCustomerId == normalizedCustomerId,
                cancellationToken);
        }

        return null;
    }

    private static async Task<Subscription?> FetchSubscriptionForTenantAsync(Tenant tenant, CancellationToken cancellationToken)
    {
        var subscriptionService = new SubscriptionService();

        var subscriptionId = (tenant.StripeSubscriptionId ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(subscriptionId))
        {
            try
            {
                return await subscriptionService.GetAsync(subscriptionId, cancellationToken: cancellationToken);
            }
            catch (StripeException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        var customerId = (tenant.StripeCustomerId ?? "").Trim();
        if (string.IsNullOrWhiteSpace(customerId))
            return null;

        var list = await subscriptionService.ListAsync(new SubscriptionListOptions
        {
            Customer = customerId,
            Status = "all",
            Limit = 3
        }, cancellationToken: cancellationToken);

        return list.Data.FirstOrDefault();
    }
}

public sealed record StripeReconciliationSummary(
    int TotalTenants,
    int CheckedTenants,
    int UpdatedTenants,
    int FailedTenants);
