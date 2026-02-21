using Microsoft.EntityFrameworkCore;
using Workshop.Data;
using Workshop.Models;

namespace Workshop.Services;

public sealed class StoreCommandService
{
    private readonly IDbContextFactory<WorkshopDbContext> _factory;

    public StoreCommandService(IDbContextFactory<WorkshopDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<bool> SaveAsync(int tenantId, Store store)
    {
        store.TenantId = tenantId;

        await using var db = await _factory.CreateDbContextAsync();
        var exists = store.Id > 0 && await db.Stores.AnyAsync(s => s.Id == store.Id && s.TenantId == tenantId);
        if (exists)
            db.Stores.Update(store);
        else
            db.Stores.Add(store);

        await db.SaveChangesAsync();
        return !exists;
    }

    public async Task<bool> DeleteAsync(int tenantId, int storeId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var store = await db.Stores.FirstOrDefaultAsync(s => s.Id == storeId && s.TenantId == tenantId);
        if (store is null)
            return false;

        var bookings = await db.Bookings
            .Where(b => b.TenantId == tenantId && b.StoreId == storeId)
            .ToListAsync();
        if (bookings.Count > 0)
            db.Bookings.RemoveRange(bookings);

        var timeOff = await db.MechanicTimeOffEntries
            .Where(t => t.TenantId == tenantId && t.StoreId == storeId)
            .ToListAsync();
        if (timeOff.Count > 0)
            db.MechanicTimeOffEntries.RemoveRange(timeOff);

        db.Stores.Remove(store);
        await db.SaveChangesAsync();
        return true;
    }
}
