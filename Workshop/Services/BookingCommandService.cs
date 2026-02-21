using Microsoft.EntityFrameworkCore;
using Workshop.Data;
using Workshop.Models;

namespace Workshop.Services;

public sealed class BookingCommandService
{
    private readonly IDbContextFactory<WorkshopDbContext> _factory;
    private readonly TenantContext _tenantContext;

    public BookingCommandService(IDbContextFactory<WorkshopDbContext> factory, TenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public async Task AddAsync(Booking booking)
    {
        booking.TenantId = _tenantContext.TenantId;
        BookingInsertPolicy.NormalizeForInsert(booking);

        await using var db = await _factory.CreateDbContextAsync();
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Booking booking)
    {
        booking.TenantId = _tenantContext.TenantId;

        await using var db = await _factory.CreateDbContextAsync();
        db.Bookings.Update(booking);
        await db.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int bookingId)
    {
        var tenantId = _tenantContext.TenantId;

        await using var db = await _factory.CreateDbContextAsync();
        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId && b.TenantId == tenantId);
        if (booking is null)
            return false;

        db.Bookings.Remove(booking);
        await db.SaveChangesAsync();
        return true;
    }
}
