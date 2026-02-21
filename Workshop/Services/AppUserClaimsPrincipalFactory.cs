using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Workshop.Models;

namespace Workshop.Services;

public sealed class AppUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<AppUser, AppRole>
{
    public AppUserClaimsPrincipalFactory(
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, roleManager, optionsAccessor)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim("tenant_id", user.TenantId.ToString()));
        if (!string.IsNullOrWhiteSpace(user.FullName))
            identity.AddClaim(new Claim("full_name", user.FullName));
        return identity;
    }
}
