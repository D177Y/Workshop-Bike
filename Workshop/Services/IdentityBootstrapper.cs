using Microsoft.AspNetCore.Identity;
using Workshop.Models;

namespace Workshop.Services;

public sealed class IdentityBootstrapper
{
    private readonly RoleManager<AppRole> _roleManager;
    private readonly UserManager<AppUser> _userManager;
    private readonly IPasswordHasher<AppUser> _passwordHasher;

    public IdentityBootstrapper(
        RoleManager<AppRole> roleManager,
        UserManager<AppUser> userManager,
        IPasswordHasher<AppUser> passwordHasher)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _passwordHasher = passwordHasher;
    }

    public async Task EnsureRolesAsync()
    {
        var roles = new[] { "SuperAdmin", "Admin", "Manager", "User" };
        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new AppRole { Name = role });
        }

        await EnsureSuperAdminUserAsync();
    }

    private async Task EnsureSuperAdminUserAsync()
    {
        var user = await _userManager.FindByNameAsync("Admin");
        if (user is null)
        {
            user = new AppUser
            {
                UserName = "Admin",
                Email = "admin@velolabs.local",
                EmailConfirmed = true,
                FullName = "Super Admin",
                TenantId = 0,
                IsActive = true
            };

            var created = await _userManager.CreateAsync(user);
            if (!created.Succeeded)
                return;
        }

        // Explicitly set the requested default credential for local setup.
        user.PasswordHash = _passwordHasher.HashPassword(user, "Admin");
        await _userManager.UpdateAsync(user);

        if (!await _userManager.IsInRoleAsync(user, "SuperAdmin"))
            await _userManager.AddToRoleAsync(user, "SuperAdmin");
    }
}
