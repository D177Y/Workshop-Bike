using Microsoft.EntityFrameworkCore;

namespace Workshop.Data;

public sealed class DatabaseInitializer
{
    private readonly IDbContextFactory<WorkshopDbContext> _factory;
    private readonly bool _seedDemoTenant;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _initialized;

    public DatabaseInitializer(IDbContextFactory<WorkshopDbContext> factory, IConfiguration configuration)
    {
        _factory = factory;
        _seedDemoTenant = configuration.GetValue("Seed:EnableDemoTenant", false);
    }

    public async Task EnsureInitializedAsync()
    {
        if (_initialized) return;

        await _lock.WaitAsync();
        try
        {
            if (_initialized) return;

            await using var db = await _factory.CreateDbContextAsync();
            await db.Database.MigrateAsync();

            if (_seedDemoTenant && !await db.Tenants.AnyAsync())
            {
                var tenant = SeedData.DefaultTenant;
                db.Tenants.Add(tenant);
                db.CatalogSettings.Add(SeedData.DefaultCatalogSettings(tenant.Id));
                db.JobDefinitions.AddRange(SeedData.Jobs(tenant.Id));
                db.AddOnDefinitions.AddRange(SeedData.AddOns(tenant.Id));
                db.AddOnRules.AddRange(SeedData.AddOnRules(tenant.Id));
                db.Stores.AddRange(SeedData.Stores(tenant.Id));
                db.Mechanics.AddRange(SeedData.Mechanics(tenant.Id));
                db.Bookings.AddRange(SeedData.Bookings(tenant.Id));
                db.BookingStatuses.AddRange(SeedData.BookingStatuses(tenant.Id));
                await db.SaveChangesAsync();
            }

            if (!await db.GlobalServiceCategories.AnyAsync())
            {
                db.GlobalServiceCategories.AddRange(SeedData.GlobalServiceCategories());
                await db.SaveChangesAsync();
            }

            if (!await db.GlobalServiceTemplates.AnyAsync())
            {
                db.GlobalServiceTemplates.AddRange(SeedData.GlobalServiceTemplates());
                await db.SaveChangesAsync();
            }

            _initialized = true;
        }
        finally
        {
            _lock.Release();
        }
    }
}
