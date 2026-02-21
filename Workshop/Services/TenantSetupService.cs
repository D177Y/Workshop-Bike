using Microsoft.EntityFrameworkCore;
using Workshop.Data;
using Workshop.Models;

namespace Workshop.Services;

public sealed class TenantSetupService
{
    private readonly WorkshopData _data;
    private readonly JobCatalogService _catalog;
    private readonly IDbContextFactory<WorkshopDbContext> _dbFactory;
    private readonly TenantContext _tenantContext;

    public TenantSetupService(
        WorkshopData data,
        JobCatalogService catalog,
        IDbContextFactory<WorkshopDbContext> dbFactory,
        TenantContext tenantContext)
    {
        _data = data;
        _catalog = catalog;
        _dbFactory = dbFactory;
        _tenantContext = tenantContext;
    }

    public async Task<TenantSetupStatus> GetStatusAsync()
    {
        await _data.EnsureInitializedAsync();
        await _catalog.EnsureInitializedAsync();

        var tenantId = _tenantContext.TenantId;
        await using var db = await _dbFactory.CreateDbContextAsync();
        var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId);

        var hasStore = _data.Stores.Count > 0;
        var storeIds = _data.Stores.Select(s => s.Id).ToHashSet();
        var hasMechanic = hasStore && _data.Mechanics.Any(m => storeIds.Contains(m.StoreId));
        var hasService = _catalog.Jobs.Count > 0;
        var profileComplete = tenant is not null && IsProfileComplete(tenant);

        var steps = new List<SetupStepStatus>
        {
            new(
                "store",
                "Add a store",
                "Create your first workshop location and opening hours.",
                "/manage-stores",
                "Manage stores",
                hasStore),
            new(
                "mechanic",
                "Add a mechanic",
                "Create at least one mechanic and assign them to a store.",
                "/manage-mechanics",
                "Manage mechanics",
                hasMechanic),
            new(
                "services",
                "Add services",
                "Create at least one service job your workshop can book, or use 1-click suggested imports.",
                "/manage-jobs",
                "Manage services",
                hasService),
            new(
                "profile",
                "Complete profile",
                "Finish your business profile details.",
                "/profile",
                "Complete profile",
                profileComplete)
        };

        return new TenantSetupStatus(steps, hasStore, hasMechanic, hasService, profileComplete);
    }

    private static bool IsProfileComplete(Tenant tenant)
    {
        return !string.IsNullOrWhiteSpace(tenant.ContactName)
               && !string.IsNullOrWhiteSpace(tenant.ContactEmail)
               && !string.IsNullOrWhiteSpace(tenant.ContactPhone)
               && !string.IsNullOrWhiteSpace(tenant.AddressLine1)
               && !string.IsNullOrWhiteSpace(tenant.City)
               && !string.IsNullOrWhiteSpace(tenant.Postcode)
               && !string.IsNullOrWhiteSpace(tenant.Country);
    }
}

public sealed record SetupStepStatus(
    string Key,
    string Title,
    string Description,
    string ActionUrl,
    string ActionText,
    bool IsComplete);

public sealed class TenantSetupStatus
{
    public static TenantSetupStatus Empty { get; } = new(Array.Empty<SetupStepStatus>(), false, false, false, false);

    public TenantSetupStatus(
        IReadOnlyList<SetupStepStatus> steps,
        bool hasStore,
        bool hasMechanic,
        bool hasService,
        bool profileComplete)
    {
        Steps = steps;
        HasStore = hasStore;
        HasMechanic = hasMechanic;
        HasService = hasService;
        ProfileComplete = profileComplete;
    }

    public IReadOnlyList<SetupStepStatus> Steps { get; }
    public bool HasStore { get; }
    public bool HasMechanic { get; }
    public bool HasService { get; }
    public bool ProfileComplete { get; }

    public int CompletedSteps => Steps.Count(s => s.IsComplete);
    public int TotalSteps => Steps.Count;
    public bool IsComplete => TotalSteps > 0 && CompletedSteps == TotalSteps;
    public SetupStepStatus? NextIncompleteStep => Steps.FirstOrDefault(s => !s.IsComplete);

    public bool CanCreateBooking => HasStore && HasMechanic && HasService;
    public bool CanViewSchedules => HasStore;
    public bool CanManageMechanics => HasStore;
}
