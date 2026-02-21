using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Workshop.Data;

public sealed class WorkshopDbContextFactory : IDesignTimeDbContextFactory<WorkshopDbContext>
{
    public WorkshopDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .AddUserSecrets<Program>(optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("WorkshopDb");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Connection string 'WorkshopDb' is not configured.");

        var optionsBuilder = new DbContextOptionsBuilder<WorkshopDbContext>();
        optionsBuilder.UseMySql(
            connectionString,
            new MySqlServerVersion(new Version(8, 0, 36)));

        return new WorkshopDbContext(optionsBuilder.Options);
    }
}
