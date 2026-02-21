using Microsoft.EntityFrameworkCore;
using Workshop.Data;
using Workshop.Models;

namespace Workshop.Services;

public sealed class TimeOffCommandService
{
    private readonly IDbContextFactory<WorkshopDbContext> _factory;
    private readonly TenantContext _tenantContext;

    public TimeOffCommandService(IDbContextFactory<WorkshopDbContext> factory, TenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public async Task<MechanicTimeOff> SaveAsync(MechanicTimeOff entry)
    {
        var normalized = Normalize(entry);
        normalized.TenantId = _tenantContext.TenantId;

        await using var db = await _factory.CreateDbContextAsync();
        var exists = normalized.Id > 0
            && await db.MechanicTimeOffEntries.AnyAsync(t => t.Id == normalized.Id && t.TenantId == normalized.TenantId);

        if (exists)
            db.MechanicTimeOffEntries.Update(normalized);
        else
            db.MechanicTimeOffEntries.Add(normalized);

        await db.SaveChangesAsync();
        return normalized;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var tenantId = _tenantContext.TenantId;

        await using var db = await _factory.CreateDbContextAsync();
        var existing = await db.MechanicTimeOffEntries.FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId);
        if (existing is null)
            return false;

        db.MechanicTimeOffEntries.Remove(existing);
        await db.SaveChangesAsync();
        return true;
    }

    private static MechanicTimeOff Normalize(MechanicTimeOff entry)
    {
        var normalized = new MechanicTimeOff
        {
            Id = entry.Id,
            TenantId = entry.TenantId,
            StoreId = entry.StoreId,
            MechanicId = entry.MechanicId,
            Type = string.IsNullOrWhiteSpace(entry.Type) ? "Holiday" : entry.Type.Trim(),
            Start = entry.Start,
            End = entry.End,
            IsAllDay = entry.IsAllDay,
            Notes = (entry.Notes ?? "").Trim(),
            Source = string.IsNullOrWhiteSpace(entry.Source) ? "Manual" : entry.Source.Trim(),
            ExternalId = (entry.ExternalId ?? "").Trim(),
            LastSyncedUtc = entry.LastSyncedUtc
        };

        if (normalized.End < normalized.Start)
            (normalized.Start, normalized.End) = (normalized.End, normalized.Start);

        return normalized;
    }
}
