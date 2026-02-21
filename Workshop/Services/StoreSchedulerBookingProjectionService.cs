using Workshop.Models;

namespace Workshop.Services;

public sealed class StoreSchedulerBookingProjectionService
{
    private readonly JobCatalogService _catalog;

    public StoreSchedulerBookingProjectionService(JobCatalogService catalog)
    {
        _catalog = catalog;
    }

    public string BuildCustomerName(Booking booking)
    {
        var first = (booking.CustomerFirstName ?? "").Trim();
        var last = (booking.CustomerLastName ?? "").Trim();
        var name = string.Join(" ", new[] { first, last }.Where(n => !string.IsNullOrWhiteSpace(n))).Trim();
        return string.IsNullOrWhiteSpace(name) ? "-" : name;
    }

    public bool IsCustomPackage(Booking booking)
    {
        if (!string.IsNullOrWhiteSpace(ResolveServicePackageId(booking)))
            return false;

        if (booking.JobIds is { Length: > 1 })
            return true;

        return booking.Title.Equals("Custom package", StringComparison.OrdinalIgnoreCase);
    }

    public List<string> BuildServiceLines(Booking booking)
    {
        if (booking.JobCard is { Services.Count: > 0 })
        {
            var cardLines = booking.JobCard.Services
                .Where(service => !IsPackageChecklistRow(service))
                .Select(service =>
                {
                    var name = string.IsNullOrWhiteSpace(service.Name) ? "Service" : service.Name.Trim();
                    var status = service.IsCompleted ? "[Done]" : "[ ]";
                    var price = service.EstimatedPriceIncVat > 0 ? $" · £{service.EstimatedPriceIncVat:0.00}" : "";
                    var mins = service.EstimatedMinutes > 0 ? $" · {service.EstimatedMinutes} mins" : "";
                    return $"{status} {name}{mins}{price}";
                })
                .ToList();

            if (cardLines.Count > 0)
                return cardLines;
        }

        var jobIds = ResolveBookingJobIds(booking);
        if (jobIds.Count == 0)
            return new List<string>();

        var servicePackageId = ResolveServicePackageId(booking);
        var lines = new List<string>();
        foreach (var jobId in jobIds)
        {
            var job = _catalog.Jobs.FirstOrDefault(j => j.Id == jobId);
            if (job is null)
                continue;

            var packageContext = _catalog.IsServicePackage(job.Id) ? null : servicePackageId;
            var (mins, price, _) = _catalog.PriceAndTime(job.Id, packageContext);
            lines.Add($"{job.Name}  {mins} mins · £{price:0.00}");
        }

        return lines;
    }

    public string BuildBookingNotes(Booking booking)
    {
        if (booking.JobCard is not null)
        {
            var service = (booking.JobCard.ServiceNotes ?? "").Trim();
            var customer = (booking.JobCard.CustomerNotes ?? "").Trim();

            if (!string.IsNullOrWhiteSpace(service) && !string.IsNullOrWhiteSpace(customer))
                return $"Service: {service} | Customer: {customer}";
            if (!string.IsNullOrWhiteSpace(service))
                return $"Service: {service}";
            if (!string.IsNullOrWhiteSpace(customer))
                return $"Customer: {customer}";
        }

        return string.IsNullOrWhiteSpace(booking.Notes) ? "-" : booking.Notes.Trim();
    }

    public List<string> ResolveBookingJobIds(Booking booking)
    {
        var ids = new List<string>();
        if (booking.JobIds is { Length: > 0 })
            ids.AddRange(booking.JobIds);
        else if (!string.IsNullOrWhiteSpace(booking.JobId))
            ids.Add(booking.JobId);

        if (booking.AddOnIds is { Length: > 0 })
        {
            foreach (var addOnId in booking.AddOnIds)
            {
                var addOn = _catalog.AddOns.FirstOrDefault(a => a.Id.Equals(addOnId, StringComparison.OrdinalIgnoreCase));
                if (addOn is null)
                    continue;

                var mappedJobId = _catalog.Jobs
                    .Where(j => !_catalog.IsServicePackage(j.Id))
                    .FirstOrDefault(j => j.Name.Equals(addOn.Name, StringComparison.OrdinalIgnoreCase))
                    ?.Id;

                if (string.IsNullOrWhiteSpace(mappedJobId))
                    continue;

                if (!ids.Contains(mappedJobId, StringComparer.OrdinalIgnoreCase))
                    ids.Add(mappedJobId);
            }
        }

        return ids
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public string? ResolveServicePackageId(Booking booking)
        => _catalog.ResolveServicePackageId(ResolveBookingJobIds(booking), booking.JobId);

    private static bool IsPackageChecklistRow(JobCardServiceItem service)
    {
        if (service.IsPackageChecklistItem)
            return true;

        return !string.IsNullOrWhiteSpace((service.ParentPackageServiceItemId ?? "").Trim());
    }
}
