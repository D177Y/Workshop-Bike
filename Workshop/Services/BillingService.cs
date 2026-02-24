using Stripe;
using Stripe.Checkout;
using Microsoft.EntityFrameworkCore;
using Workshop.Data;
using Workshop.Models;

namespace Workshop.Services;

public sealed class BillingService
{
    private readonly IConfiguration _config;
    private readonly IDbContextFactory<WorkshopDbContext> _dbFactory;

    public BillingService(IConfiguration config, IDbContextFactory<WorkshopDbContext> dbFactory)
    {
        _config = config;
        _dbFactory = dbFactory;
    }

    public Session CreateCheckoutSession(int tenantId, string email, Workshop.Models.PlanTier plan, string baseUrl, bool annualBilling = false)
    {
        var priceId = GetPriceId(plan, annualBilling);
        var mechanicLimit = GetMechanicLimit(plan);

        var options = new SessionCreateOptions
        {
            Mode = "subscription",
            CustomerEmail = email,
            SuccessUrl = $"{baseUrl}billing/success",
            CancelUrl = $"{baseUrl}billing/cancel",
            LineItems = new List<SessionLineItemOptions>
            {
                new() { Price = priceId, Quantity = 1 }
            },
            SubscriptionData = new SessionSubscriptionDataOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    ["tenant_id"] = tenantId.ToString(),
                    ["plan"] = plan.ToString().ToLowerInvariant(),
                    ["mechanic_limit"] = mechanicLimit.ToString(),
                    ["billing_cycle"] = annualBilling ? "annual" : "monthly"
                }
            }
        };

        var service = new SessionService();
        return service.Create(options);
    }

    public string GetPriceId(Workshop.Models.PlanTier plan, bool annualBilling = false)
    {
        var section = _config.GetSection("Stripe:Prices");
        var starter = annualBilling ? section["StarterAnnual"] ?? section["Starter"] : section["Starter"];
        var standard = annualBilling ? section["StandardAnnual"] ?? section["Standard"] : section["Standard"];
        var premium = annualBilling ? section["PremiumAnnual"] ?? section["Premium"] : section["Premium"];
        var enterprise = annualBilling ? section["EnterpriseAnnual"] ?? section["Enterprise"] : section["Enterprise"];

        return plan switch
        {
            Workshop.Models.PlanTier.Starter => starter ?? "",
            Workshop.Models.PlanTier.Standard => standard ?? "",
            Workshop.Models.PlanTier.Premium => premium ?? "",
            Workshop.Models.PlanTier.Enterprise => enterprise ?? "",
            _ => standard ?? ""
        };
    }

    public Workshop.Models.PlanTier? ResolvePlanFromPriceId(string? priceId)
    {
        var normalized = (priceId ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return null;

        foreach (var plan in PlanCatalog.Ordered)
        {
            var monthlyPriceId = (GetPriceId(plan.Tier, annualBilling: false) ?? "").Trim();
            var annualPriceId = (GetPriceId(plan.Tier, annualBilling: true) ?? "").Trim();
            if (string.Equals(normalized, monthlyPriceId, StringComparison.Ordinal)
                || string.Equals(normalized, annualPriceId, StringComparison.Ordinal))
            {
                return plan.Tier;
            }
        }

        return null;
    }

    public int GetMechanicLimit(Workshop.Models.PlanTier plan)
        => PlanCatalog.GetMechanicLimit(plan);

    public async Task<PlanUpgradeResult> UpgradeSubscriptionAsync(int tenantId, string email, Workshop.Models.PlanTier targetPlan, string baseUrl)
    {
        var priceId = GetPriceId(targetPlan);
        if (string.IsNullOrWhiteSpace(priceId))
        {
            return PlanUpgradeResult.Fail("This plan is not configured for self-serve upgrade yet.");
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant is null)
        {
            return PlanUpgradeResult.Fail("Tenant not found for billing.");
        }

        var subscriptionId = (tenant.StripeSubscriptionId ?? "").Trim();
        if (string.IsNullOrWhiteSpace(subscriptionId) && !string.IsNullOrWhiteSpace(tenant.StripeCustomerId))
        {
            var subscriptionService = new SubscriptionService();
            var active = subscriptionService.List(new SubscriptionListOptions
            {
                Customer = tenant.StripeCustomerId,
                Status = "active",
                Limit = 1
            });

            var activeSub = active.Data.FirstOrDefault();
            if (activeSub is not null)
            {
                subscriptionId = activeSub.Id;
                tenant.StripeSubscriptionId = activeSub.Id;
                await db.SaveChangesAsync();
            }
        }

        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            var session = CreateCheckoutSession(tenantId, email, targetPlan, baseUrl);
            return PlanUpgradeResult.Redirect(session.Url, "No active subscription found. Redirecting to checkout.");
        }

        try
        {
            var subscriptionService = new SubscriptionService();
            var subscription = subscriptionService.Get(subscriptionId);
            var item = subscription.Items?.Data?.FirstOrDefault();
            if (item is null)
                return PlanUpgradeResult.Fail("Current Stripe subscription item was not found.");

            if (string.Equals(item.Price?.Id, priceId, StringComparison.Ordinal))
            {
                tenant.Plan = targetPlan;
                tenant.MaxMechanics = GetMechanicLimit(targetPlan);
                await db.SaveChangesAsync();
                return PlanUpgradeResult.Success("Your plan is already set to this tier.");
            }

            var updated = subscriptionService.Update(subscriptionId, new SubscriptionUpdateOptions
            {
                Items = new List<SubscriptionItemOptions>
                {
                    new()
                    {
                        Id = item.Id,
                        Price = priceId
                    }
                },
                BillingCycleAnchor = SubscriptionBillingCycleAnchor.Unchanged,
                ProrationBehavior = "create_prorations"
            });

            tenant.Plan = targetPlan;
            tenant.MaxMechanics = GetMechanicLimit(targetPlan);
            tenant.StripeCustomerId = (updated.CustomerId ?? tenant.StripeCustomerId ?? "").Trim();
            tenant.StripeSubscriptionId = (updated.Id ?? tenant.StripeSubscriptionId ?? "").Trim();
            tenant.IsActive = true;
            await db.SaveChangesAsync();

            return PlanUpgradeResult.Success("Plan upgraded. Stripe applies proration, so you only pay the difference for the remaining period.");
        }
        catch (StripeException ex)
        {
            var detail = ex.StripeError?.Message;
            return PlanUpgradeResult.Fail(string.IsNullOrWhiteSpace(detail)
                ? "Stripe could not process the upgrade right now."
                : detail);
        }
    }

    public sealed record PlanUpgradeResult(bool IsSuccess, bool RequiresRedirect, string? RedirectUrl, string Message)
    {
        public static PlanUpgradeResult Success(string message) => new(true, false, null, message);
        public static PlanUpgradeResult Redirect(string? url, string message) => new(true, true, url, message);
        public static PlanUpgradeResult Fail(string message) => new(false, false, null, message);
    }
}
