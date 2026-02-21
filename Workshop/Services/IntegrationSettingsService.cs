using Microsoft.EntityFrameworkCore;
using Workshop.Data;
using Workshop.Models;

namespace Workshop.Services;

public sealed class IntegrationSettingsService
{
    private readonly IDbContextFactory<WorkshopDbContext> _factory;
    private readonly TenantContext _tenantContext;

    public IntegrationSettingsService(IDbContextFactory<WorkshopDbContext> factory, TenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public async Task<IntegrationSettings> GetAsync()
    {
        var tenantId = _tenantContext.TenantId;
        await using var db = await _factory.CreateDbContextAsync();
        var existing = await db.IntegrationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId);

        return existing is null ? CreateDefault(tenantId) : Normalize(existing);
    }

    public async Task<IntegrationSettings> SaveAsync(IntegrationSettings settings)
    {
        var tenantId = _tenantContext.TenantId;
        var normalized = Normalize(settings);
        normalized.TenantId = tenantId;

        await using var db = await _factory.CreateDbContextAsync();
        if (normalized.TimetasticEnabled && !string.IsNullOrWhiteSpace(normalized.TimetasticWebhookSecret))
        {
            var secret = normalized.TimetasticWebhookSecret;
            var secretInUseByAnotherTenant = await db.IntegrationSettings
                .AnyAsync(x => x.TenantId != tenantId
                               && x.TimetasticEnabled
                               && x.TimetasticWebhookSecret == secret);
            if (secretInUseByAnotherTenant)
                throw new InvalidOperationException("Timetastic webhook secret is already used by another enabled tenant. Use a unique secret per tenant.");
        }

        var existing = await db.IntegrationSettings
            .FirstOrDefaultAsync(x => x.TenantId == tenantId);

        if (existing is null)
        {
            db.IntegrationSettings.Add(normalized);
        }
        else
        {
            normalized.Id = existing.Id;
            db.Entry(existing).CurrentValues.SetValues(normalized);
            existing.TimetasticMechanicMappings = normalized.TimetasticMechanicMappings;
        }

        await db.SaveChangesAsync();
        return normalized;
    }

    private static IntegrationSettings CreateDefault(int tenantId)
        => Normalize(new IntegrationSettings
        {
            TenantId = tenantId
        });

    private static IntegrationSettings Normalize(IntegrationSettings settings)
    {
        var normalized = new IntegrationSettings
        {
            Id = settings.Id,
            TenantId = settings.TenantId,
            TimetasticEnabled = settings.TimetasticEnabled,
            TimetasticApiBaseUrl = (settings.TimetasticApiBaseUrl ?? "").Trim(),
            TimetasticApiToken = (settings.TimetasticApiToken ?? "").Trim(),
            TimetasticWebhookSecret = (settings.TimetasticWebhookSecret ?? "").Trim(),
            TimetasticWebhookCallbackUrl = (settings.TimetasticWebhookCallbackUrl ?? "").Trim(),
            TimetasticLastSyncUtc = settings.TimetasticLastSyncUtc,
            TimetasticLastWebhookReceivedUtc = settings.TimetasticLastWebhookReceivedUtc,
            TimetasticMechanicMappings = (settings.TimetasticMechanicMappings ?? new List<TimetasticMechanicMapping>())
                .Where(m => m.MechanicId > 0)
                .GroupBy(m => m.MechanicId)
                .Select(g =>
                {
                    var first = g.First();
                    return new TimetasticMechanicMapping
                    {
                        MechanicId = g.Key,
                        TimetasticUserId = (first.TimetasticUserId ?? "").Trim(),
                        TimetasticUserName = (first.TimetasticUserName ?? "").Trim()
                    };
                })
                .OrderBy(m => m.MechanicId)
                .ToList()
        };

        if (string.IsNullOrWhiteSpace(normalized.TimetasticApiBaseUrl))
            normalized.TimetasticApiBaseUrl = "https://app.timetastic.co.uk/api/";

        return normalized;
    }
}
