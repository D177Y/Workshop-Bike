using Microsoft.AspNetCore.Identity;

namespace Workshop.Models;

public sealed class AppUser : IdentityUser<int>
{
    public int TenantId { get; set; }
    public string FullName { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginUtc { get; set; }
}
