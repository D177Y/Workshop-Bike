using Microsoft.EntityFrameworkCore;
using Workshop.Data;
using Workshop.Models;

namespace Workshop.Services;

public sealed class MechanicCommandService
{
    private readonly IDbContextFactory<WorkshopDbContext> _factory;

    public MechanicCommandService(IDbContextFactory<WorkshopDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<bool> SaveAsync(int tenantId, Mechanic mechanic)
    {
        mechanic.TenantId = tenantId;

        await using var db = await _factory.CreateDbContextAsync();
        var exists = mechanic.Id > 0 && await db.Mechanics.AnyAsync(m => m.Id == mechanic.Id && m.TenantId == tenantId);
        if (!exists)
        {
            var tenantLimit = await db.Tenants
                .Where(t => t.Id == tenantId)
                .Select(t => t.MaxMechanics)
                .FirstOrDefaultAsync();

            if (tenantLimit > 0)
            {
                var currentCount = await db.Mechanics.CountAsync(m => m.TenantId == tenantId);
                if (currentCount >= tenantLimit)
                    throw new InvalidOperationException($"Mechanic limit reached ({tenantLimit}).");
            }
        }

        if (exists)
            db.Mechanics.Update(mechanic);
        else
            db.Mechanics.Add(mechanic);

        await db.SaveChangesAsync();
        return !exists;
    }

    public async Task<bool> DeleteAsync(int tenantId, int mechanicId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var mechanic = await db.Mechanics.FirstOrDefaultAsync(m => m.Id == mechanicId && m.TenantId == tenantId);
        if (mechanic is null)
            return false;

        var bookings = await db.Bookings
            .Where(b => b.TenantId == tenantId && b.MechanicId == mechanicId)
            .ToListAsync();
        if (bookings.Count > 0)
            db.Bookings.RemoveRange(bookings);

        var timeOff = await db.MechanicTimeOffEntries
            .Where(t => t.TenantId == tenantId && t.MechanicId == mechanicId)
            .ToListAsync();
        if (timeOff.Count > 0)
            db.MechanicTimeOffEntries.RemoveRange(timeOff);

        db.Mechanics.Remove(mechanic);
        await db.SaveChangesAsync();
        return true;
    }
}
