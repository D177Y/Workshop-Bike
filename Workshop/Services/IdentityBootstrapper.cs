using Microsoft.AspNetCore.Identity;
using Workshop.Models;

namespace Workshop.Services;

public sealed class IdentityBootstrapper
{
    private readonly RoleManager<AppRole> _roleManager;
    private readonly UserManager<AppUser> _userManager;
    private readonly IPasswordHasher<AppUser> _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<IdentityBootstrapper> _logger;

    public IdentityBootstrapper(
        RoleManager<AppRole> roleManager,
        UserManager<AppUser> userManager,
        IPasswordHasher<AppUser> passwordHasher,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<IdentityBootstrapper> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
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
        var settings = _configuration.GetSection("SuperAdmin");
        var enabled = settings.GetValue("Enabled", true);
        if (!enabled)
            return;

        var userName = (settings["UserName"] ?? "Admin").Trim();
        var email = (settings["Email"] ?? "admin@velolabs.local").Trim();
        var bootstrapPassword = (settings["BootstrapPassword"] ?? "").Trim();
        var resetPasswordOnStartup = settings.GetValue("ResetPasswordOnStartup", false);
        var allowInProduction = settings.GetValue("AllowInProduction", false);

        if (_environment.IsProduction() && !allowInProduction)
        {
            _logger.LogInformation("SuperAdmin bootstrap is disabled in production.");
            return;
        }

        if (string.IsNullOrWhiteSpace(userName))
        {
            _logger.LogWarning("SuperAdmin bootstrap skipped because username is empty.");
            return;
        }

        var user = await _userManager.FindByNameAsync(userName);
        if (user is null)
        {
            if (!CanUseBootstrapPassword(bootstrapPassword))
            {
                _logger.LogWarning("SuperAdmin bootstrap user not created because password is not configured securely.");
                return;
            }

            user = new AppUser
            {
                UserName = userName,
                Email = email,
                EmailConfirmed = true,
                FullName = "Super Admin",
                TenantId = 0,
                IsActive = true
            };

            var created = await _userManager.CreateAsync(user, bootstrapPassword);
            if (!created.Succeeded)
            {
                _logger.LogError(
                    "SuperAdmin bootstrap user creation failed: {Errors}",
                    string.Join(" | ", created.Errors.Select(error => error.Description)));
                return;
            }

            _logger.LogInformation("SuperAdmin bootstrap user created for username {UserName}.", userName);
        }
        else
        {
            var changed = false;
            if (!string.Equals(user.Email ?? "", email, StringComparison.OrdinalIgnoreCase))
            {
                user.Email = email;
                changed = true;
            }

            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                changed = true;
            }

            if (resetPasswordOnStartup && CanUseBootstrapPassword(bootstrapPassword))
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, bootstrapPassword);
                changed = true;
                _logger.LogWarning("SuperAdmin password reset from configuration on startup.");
            }

            if (changed)
                await _userManager.UpdateAsync(user);
        }

        if (!await _userManager.IsInRoleAsync(user, "SuperAdmin"))
            await _userManager.AddToRoleAsync(user, "SuperAdmin");
    }

    private bool CanUseBootstrapPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        if (password.Equals("__SET_IN_USER_SECRETS__", StringComparison.OrdinalIgnoreCase))
            return false;

        if (password.Equals("Admin", StringComparison.OrdinalIgnoreCase) && !_environment.IsDevelopment())
            return false;

        return true;
    }
}
