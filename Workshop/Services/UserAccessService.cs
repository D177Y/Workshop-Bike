using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Workshop.Data;
using Workshop.Models;

namespace Workshop.Services;

public sealed class UserAccessScope
{
    public UserAccessScope(IEnumerable<int> allowedStoreIds, IEnumerable<int> allowedMechanicIds)
    {
        AllowedStoreIds = new HashSet<int>(allowedStoreIds);
        AllowedMechanicIds = new HashSet<int>(allowedMechanicIds);
    }

    public HashSet<int> AllowedStoreIds { get; }
    public HashSet<int> AllowedMechanicIds { get; }

    public bool HasStoreRestrictions => AllowedStoreIds.Count > 0;
    public bool HasMechanicRestrictions => AllowedMechanicIds.Count > 0;

    public bool CanAccessStore(int storeId)
        => !HasStoreRestrictions || AllowedStoreIds.Contains(storeId);

    public bool CanAccessMechanic(Mechanic mechanic)
        => CanAccessStore(mechanic.StoreId) && (!HasMechanicRestrictions || AllowedMechanicIds.Contains(mechanic.Id));

    public static UserAccessScope AllowAll()
        => new(Array.Empty<int>(), Array.Empty<int>());
}

public sealed class UserAccessService
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly IDbContextFactory<WorkshopDbContext> _dbFactory;
    private readonly TenantContext _tenantContext;

    public UserAccessService(
        AuthenticationStateProvider authStateProvider,
        IDbContextFactory<WorkshopDbContext> dbFactory,
        TenantContext tenantContext)
    {
        _authStateProvider = authStateProvider;
        _dbFactory = dbFactory;
        _tenantContext = tenantContext;
    }

    public async Task<UserAccessScope> GetCurrentAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var principal = authState.User;
        if (principal?.Identity?.IsAuthenticated != true)
            return UserAccessScope.AllowAll();

        var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId) || userId <= 0)
            return UserAccessScope.AllowAll();

        await using var db = await _dbFactory.CreateDbContextAsync();
        var userTenantId = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.TenantId)
            .FirstOrDefaultAsync();

        var tenantClaim = principal.FindFirstValue("tenant_id");
        var claimTenantId = int.TryParse(tenantClaim, out var parsedClaimTenantId) && parsedClaimTenantId > 0
            ? parsedClaimTenantId
            : 0;

        var tenantId = userTenantId > 0
            ? userTenantId
            : claimTenantId > 0
                ? claimTenantId
                : _tenantContext.TenantId;

        _tenantContext.SetTenantId(tenantId);

        var hasExplicitStoreRows = await db.UserStoreAccess
            .AnyAsync(x => x.UserId == userId);

        var storeIds = await (from access in db.UserStoreAccess
                              join store in db.Stores on access.StoreId equals store.Id
                              where access.UserId == userId && store.TenantId == tenantId
                              select access.StoreId)
            .Distinct()
            .ToListAsync();

        // Fallback safety: if explicit access rows exist but all are stale/invalid for this tenant,
        // treat as unrestricted instead of locking the user out.
        if (hasExplicitStoreRows && storeIds.Count == 0)
            return UserAccessScope.AllowAll();

        var mechanicIds = await (from access in db.UserMechanicAccess
                                 join mechanic in db.Mechanics on access.MechanicId equals mechanic.Id
                                 where access.UserId == userId && mechanic.TenantId == tenantId
                                 select access.MechanicId)
            .Distinct()
            .ToListAsync();

        return new UserAccessScope(storeIds, mechanicIds);
    }
}
