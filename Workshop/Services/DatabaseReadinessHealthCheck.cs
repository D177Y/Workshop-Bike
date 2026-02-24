using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Workshop.Data;

namespace Workshop.Services;

public sealed class DatabaseReadinessHealthCheck : IHealthCheck
{
    private readonly IDbContextFactory<WorkshopDbContext> _dbFactory;

    public DatabaseReadinessHealthCheck(IDbContextFactory<WorkshopDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var canConnect = await db.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
                return HealthCheckResult.Unhealthy("Database connection failed.");

            return HealthCheckResult.Healthy("Database reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database health check failed.", ex);
        }
    }
}
