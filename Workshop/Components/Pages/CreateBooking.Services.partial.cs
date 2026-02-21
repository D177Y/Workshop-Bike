using Workshop.Models;

namespace Workshop.Components.Pages;

public partial class CreateBooking
{
    private void TogglePackageService(string id, Microsoft.AspNetCore.Components.ChangeEventArgs e)
    {
        if (!CanSelectPackageServices || string.IsNullOrWhiteSpace(SelectedPackageJobId))
            return;

        if (!Catalog.IsServiceAvailableAsAdditionalService(id, SelectedPackageJobId))
        {
            SelectedPackageServices.Remove(id);
            return;
        }

        var isChecked = e?.Value is bool b && b;

        if (isChecked) SelectedPackageServices.Add(id);
        else SelectedPackageServices.Remove(id);

        if (_selectedMechanicId != 0)
        {
            var selected = Data.Mechanics.FirstOrDefault(m => m.Id == _selectedMechanicId);
            if (selected is null || !_userAccess.CanAccessMechanic(selected) || !CanMechanicDoJobs(selected, SelectedJobsForPricing))
                _selectedMechanicId = 0;
        }

        Recalc();
        UpdateEarliestAvailableDay();
        RefreshMonthAvailability();
        EnsureSelectedDayIsValid();
    }

    private void Recalc()
    {
        var selected = SelectedJobsForPricing.ToList();
        if (selected.Count == 0)
        {
            CalculatedMinutes = 0;
            CalculatedPrice = 0;
            return;
        }

        var selectedPackageId = SelectedPackageJobId;
        var totalMinutes = 0;
        var totalPrice = 0m;

        foreach (var job in selected)
        {
            var packageContext = !string.IsNullOrWhiteSpace(selectedPackageId) && !IsServicePackage(job.Id)
                ? selectedPackageId
                : null;
            var (mins, price, _) = Catalog.PriceAndTime(job.Id, packageContext);
            totalMinutes += mins;
            totalPrice += price;
        }

        CalculatedMinutes = totalMinutes;
        CalculatedPrice = totalPrice;
    }

    private JobDefinition? SelectedServicePackage =>
        SelectedJobs.FirstOrDefault(job => IsServicePackage(job.Id));

    private string? SelectedPackageJobId => SelectedServicePackage?.Id;

    private bool CanSelectPackageServices => SelectedServicePackage is not null;

    private bool IsJobSelected(string id) => SelectedJobIds.Contains(id);

    private void ToggleJob(JobDefinition job, Microsoft.AspNetCore.Components.ChangeEventArgs e)
    {
        var isChecked = e?.Value is bool b && b;
        var isPackage = job.Category.Equals("Service Packages", StringComparison.OrdinalIgnoreCase);

        if (isPackage)
        {
            if (isChecked)
            {
                SelectedJobIds = new List<string> { job.Id };
            }
            else
            {
                SelectedJobIds.Remove(job.Id);
            }
        }
        else
        {
            if (SelectedJobIds.Any(id => IsServicePackage(id)))
                SelectedJobIds.Clear();

            if (isChecked)
            {
                if (!SelectedJobIds.Contains(job.Id, StringComparer.OrdinalIgnoreCase))
                    SelectedJobIds.Add(job.Id);
            }
            else
                SelectedJobIds.Remove(job.Id);
        }

        if (!SelectedJobIds.Any(IsServicePackage))
        {
            SelectedPackageServices.Clear();
        }
        else
        {
            var packageId = SelectedPackageJobId;
            var validIds = Catalog.Jobs
                .Where(j => !IsServicePackage(j.Id))
                .Where(j => Catalog.IsServiceAvailableAsAdditionalService(j.Id, packageId))
                .Select(j => j.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            SelectedPackageServices.RemoveWhere(id => !validIds.Contains(id));
        }

        if (_selectedMechanicId != 0)
        {
            var selected = Data.Mechanics.FirstOrDefault(m => m.Id == _selectedMechanicId);
            if (selected is null || !_userAccess.CanAccessMechanic(selected) || !CanMechanicDoJobs(selected, SelectedJobsForPricing))
                _selectedMechanicId = 0;
        }
        Recalc();
        UpdateEarliestAvailableDay();
        RefreshMonthAvailability();
        EnsureSelectedDayIsValid();
    }

    private bool IsServicePackage(string jobId) =>
        Catalog.Jobs.Any(j => j.Id == jobId && j.Category.Equals("Service Packages", StringComparison.OrdinalIgnoreCase));

    private IReadOnlyDictionary<string, List<JobDefinition>> JobGroups
    {
        get
        {
            var filtered = Catalog.Jobs
                .Where(j => string.IsNullOrWhiteSpace(JobFilter) || j.Name.Contains(JobFilter, StringComparison.OrdinalIgnoreCase))
                .GroupBy(j => j.Category)
                .ToDictionary(g => g.Key, g => g.OrderBy(j => j.Name).ToList(), StringComparer.OrdinalIgnoreCase);

            var ordered = new List<KeyValuePair<string, List<JobDefinition>>>();
            if (filtered.TryGetValue("Service Packages", out var packages))
            {
                ordered.Add(new KeyValuePair<string, List<JobDefinition>>("Service Packages", packages));
                filtered.Remove("Service Packages");
            }

            foreach (var group in filtered.OrderBy(g => g.Key))
                ordered.Add(group);

            return ordered.ToDictionary(k => k.Key, v => v.Value, StringComparer.OrdinalIgnoreCase);
        }
    }

    private bool IsGroupOpen(string key)
    {
        if (!string.IsNullOrWhiteSpace(JobFilter))
            return true;

        if (ExpandedGroups.Count == 0)
            return key.Equals("Service Packages", StringComparison.OrdinalIgnoreCase);
        return ExpandedGroups.Contains(key);
    }

    private void ToggleGroup(string key)
    {
        if (ExpandedGroups.Count == 0)
        {
            ExpandedGroups.Add("Service Packages");
        }

        if (ExpandedGroups.Contains(key))
            ExpandedGroups.Remove(key);
        else
            ExpandedGroups.Add(key);
    }

    private string GetPackageServiceMeta(string serviceId)
    {
        if (!CanSelectPackageServices || string.IsNullOrWhiteSpace(SelectedPackageJobId))
            return "";

        var (mins, price, _) = Catalog.PriceAndTime(serviceId, SelectedPackageJobId);
        return $"{mins} mins · £{price:0.00}";
    }

    private IReadOnlyDictionary<string, List<JobDefinition>> PackageServiceGroups
    {
        get
        {
            if (!CanSelectPackageServices)
                return new Dictionary<string, List<JobDefinition>>(StringComparer.OrdinalIgnoreCase);

            var packageId = SelectedPackageJobId;
            return Catalog.Jobs
                .Where(j => !IsServicePackage(j.Id))
                .Where(j => Catalog.IsServiceAvailableAsAdditionalService(j.Id, packageId))
                .Where(j => string.IsNullOrWhiteSpace(PackageServiceFilter) || j.Name.Contains(PackageServiceFilter, StringComparison.OrdinalIgnoreCase))
                .GroupBy(j => j.Category)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.OrderBy(j => j.Name).ToList(), StringComparer.OrdinalIgnoreCase);
        }
    }

    private bool IsPackageServiceGroupOpen(string key)
    {
        if (!string.IsNullOrWhiteSpace(PackageServiceFilter))
            return true;

        if (ExpandedPackageServiceGroups.Count == 0)
        {
            var first = PackageServiceGroups.Keys.FirstOrDefault();
            return first != null && first.Equals(key, StringComparison.OrdinalIgnoreCase);
        }

        return ExpandedPackageServiceGroups.Contains(key);
    }

    private void TogglePackageServiceGroup(string key)
    {
        if (ExpandedPackageServiceGroups.Count == 0)
        {
            var first = PackageServiceGroups.Keys.FirstOrDefault();
            if (first != null)
                ExpandedPackageServiceGroups.Add(first);
        }

        if (ExpandedPackageServiceGroups.Contains(key))
            ExpandedPackageServiceGroups.Remove(key);
        else
            ExpandedPackageServiceGroups.Add(key);
    }

    private void ApplyLegacyAdditionalServicesFromQuery(string raw)
    {
        if (!CanSelectPackageServices || string.IsNullOrWhiteSpace(raw))
            return;

        SelectedPackageServices.Clear();

        foreach (var token in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var mappedServiceId = ResolveLegacyServiceId(token);
            if (!string.IsNullOrWhiteSpace(mappedServiceId)
                && Catalog.IsServiceAvailableAsAdditionalService(mappedServiceId, SelectedPackageJobId))
                SelectedPackageServices.Add(mappedServiceId);
        }
    }

    private string? ResolveLegacyServiceId(string token)
    {
        if (Catalog.Jobs.Any(j => j.Id.Equals(token, StringComparison.OrdinalIgnoreCase) && !IsServicePackage(j.Id)))
            return Catalog.Jobs.First(j => j.Id.Equals(token, StringComparison.OrdinalIgnoreCase)).Id;

        var legacy = Catalog.AddOns.FirstOrDefault(a => a.Id.Equals(token, StringComparison.OrdinalIgnoreCase));
        if (legacy is null)
            return null;

        return Catalog.Jobs
            .Where(j => !IsServicePackage(j.Id))
            .FirstOrDefault(j => j.Name.Equals(legacy.Name, StringComparison.OrdinalIgnoreCase))
            ?.Id;
    }
}
