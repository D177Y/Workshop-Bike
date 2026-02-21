using Microsoft.EntityFrameworkCore;
using Workshop.Data;
using Workshop.Models;

namespace Workshop.Services;

public sealed class WorkshopReadService
{
    private readonly IDbContextFactory<WorkshopDbContext> _factory;

    public WorkshopReadService(IDbContextFactory<WorkshopDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<WorkshopReadSnapshot> LoadSnapshotAsync(int tenantId)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var stores = await db.Stores
            .Where(s => s.TenantId == tenantId)
            .OrderBy(s => s.Name)
            .ToListAsync();

        var mechanics = await db.Mechanics
            .Where(m => m.TenantId == tenantId)
            .OrderBy(m => m.Name)
            .ToListAsync();

        var bookings = await db.Bookings
            .Where(b => b.TenantId == tenantId)
            .OrderBy(b => b.Start)
            .ToListAsync();

        var statuses = await db.BookingStatuses
            .Where(s => s.TenantId == tenantId)
            .OrderBy(s => s.Name)
            .ToListAsync();

        var timeOff = await db.MechanicTimeOffEntries
            .Where(t => t.TenantId == tenantId)
            .OrderBy(t => t.Start)
            .ToListAsync();

        var customerProfiles = await db.CustomerProfiles
            .Where(c => c.TenantId == tenantId)
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .ThenBy(c => c.AccountNumber)
            .ToListAsync();

        return new WorkshopReadSnapshot
        {
            Stores = stores,
            Mechanics = mechanics,
            Bookings = bookings,
            Statuses = statuses,
            MechanicTimeOffEntries = timeOff,
            CustomerProfiles = customerProfiles
        };
    }
}

public sealed class WorkshopReadSnapshot
{
    public List<Store> Stores { get; init; } = new();
    public List<Mechanic> Mechanics { get; init; } = new();
    public List<Booking> Bookings { get; init; } = new();
    public List<BookingStatus> Statuses { get; init; } = new();
    public List<MechanicTimeOff> MechanicTimeOffEntries { get; init; } = new();
    public List<CustomerProfile> CustomerProfiles { get; init; } = new();
}
