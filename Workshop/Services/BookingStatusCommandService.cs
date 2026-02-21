using Microsoft.EntityFrameworkCore;
using Workshop.Data;
using Workshop.Models;

namespace Workshop.Services;

public sealed class BookingStatusCommandService
{
    private readonly IDbContextFactory<WorkshopDbContext> _factory;

    public BookingStatusCommandService(IDbContextFactory<WorkshopDbContext> factory)
    {
        _factory = factory;
    }

    public async Task SaveAsync(int tenantId, BookingStatus status)
    {
        status.TenantId = tenantId;

        await using var db = await _factory.CreateDbContextAsync();
        var exists = await db.BookingStatuses.AnyAsync(s => s.TenantId == status.TenantId && s.Name == status.Name);
        if (exists)
            db.BookingStatuses.Update(status);
        else
            db.BookingStatuses.Add(status);

        await db.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int tenantId, string name)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var status = await db.BookingStatuses.FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Name == name);
        if (status is null)
            return false;

        db.BookingStatuses.Remove(status);
        await db.SaveChangesAsync();
        return true;
    }
}
