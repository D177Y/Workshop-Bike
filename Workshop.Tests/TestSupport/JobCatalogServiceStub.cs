using Workshop.Models;

namespace Workshop.Services;

public sealed class JobCatalogService
{
    public List<JobDefinition> Jobs { get; set; } = new();
    public List<AddOnDefinition> AddOns { get; set; } = new();

    public bool IsServicePackage(string jobId)
        => Jobs.Any(j => j.Id.Equals(jobId, StringComparison.OrdinalIgnoreCase)
                         && j.Category.Equals("Service Packages", StringComparison.OrdinalIgnoreCase));

    public string? ResolveServicePackageId(IEnumerable<string>? jobIds, string? fallbackJobId = null)
    {
        if (jobIds is not null)
        {
            foreach (var jobId in jobIds)
            {
                if (IsServicePackage(jobId))
                    return jobId;
            }
        }

        if (!string.IsNullOrWhiteSpace(fallbackJobId) && IsServicePackage(fallbackJobId))
            return fallbackJobId;

        return null;
    }

    public (int minutes, decimal priceIncVat, string title) PriceAndTime(string jobId, string? servicePackageJobId = null)
    {
        var job = Jobs.FirstOrDefault(j => j.Id.Equals(jobId, StringComparison.OrdinalIgnoreCase));
        if (job is null)
            return (0, 0m, "");

        return (job.DefaultMinutes, job.BasePriceIncVat, job.Name);
    }
}
