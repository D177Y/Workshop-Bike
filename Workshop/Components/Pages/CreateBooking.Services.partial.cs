using System.Globalization;
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

        SyncManualServicePricingInputs();

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

            int mins;
            decimal price;
            if (RequiresManualPricingAtUse(job))
            {
                mins = GetManualServiceMinutes(job.Id);
                price = GetManualServicePrice(job.Id);
            }
            else
            {
                var priced = Catalog.PriceAndTime(job.Id, packageContext);
                mins = priced.minutes;
                price = priced.priceIncVat;
            }

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

        SyncManualServicePricingInputs();

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

    private bool RequiresManualPricingAtUse(JobDefinition job)
        => Catalog.RequiresManualQuoteAtUse(job);

    private bool RequiresManualPricingAtUse(string jobId)
        => Catalog.RequiresManualQuoteAtUse(jobId);

    private List<JobDefinition> SelectedManualPricingJobs =>
        SelectedJobsForPricing
            .Where(RequiresManualPricingAtUse)
            .OrderBy(j => j.Name)
            .ToList();

    private bool IsManualServiceInputInvalid(string jobId)
        => GetManualServiceMinutes(jobId) <= 0 || GetManualServicePrice(jobId) <= 0m;

    private bool HasManualServiceInputErrors =>
        SelectedManualPricingJobs.Any(job => IsManualServiceInputInvalid(job.Id));

    private void SyncManualServicePricingInputs()
    {
        var selectedManualJobIds = SelectedManualPricingJobs
            .Select(j => j.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var staleIds = ManualServicePricingByJobId.Keys
            .Where(id => !selectedManualJobIds.Contains(id))
            .ToList();
        foreach (var staleId in staleIds)
            ManualServicePricingByJobId.Remove(staleId);

        foreach (var job in SelectedManualPricingJobs)
        {
            if (ManualServicePricingByJobId.ContainsKey(job.Id))
                continue;

            ManualServicePricingByJobId[job.Id] = new ManualServicePricingInput
            {
                JobId = job.Id,
                Minutes = Math.Max(0, job.DefaultMinutes),
                PriceIncVat = 0m
            };
        }
    }

    private int GetManualServiceMinutes(string jobId)
        => ManualServicePricingByJobId.TryGetValue(jobId, out var value) ? Math.Max(0, value.Minutes) : 0;

    private decimal GetManualServicePrice(string jobId)
        => ManualServicePricingByJobId.TryGetValue(jobId, out var value) ? Math.Max(0m, value.PriceIncVat) : 0m;

    private void SetManualServiceMinutes(string jobId, Microsoft.AspNetCore.Components.ChangeEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            return;

        var current = ManualServicePricingByJobId.TryGetValue(jobId, out var existing)
            ? existing
            : new ManualServicePricingInput { JobId = jobId };

        if (int.TryParse(e?.Value?.ToString(), out var minutes))
            current.Minutes = Math.Max(0, minutes);

        ManualServicePricingByJobId[jobId] = current;
        Recalc();
    }

    private void SetManualServicePrice(string jobId, Microsoft.AspNetCore.Components.ChangeEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            return;

        var current = ManualServicePricingByJobId.TryGetValue(jobId, out var existing)
            ? existing
            : new ManualServicePricingInput { JobId = jobId };

        var raw = (e?.Value?.ToString() ?? "").Trim();
        if (decimal.TryParse(raw, out var price)
            || decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out price))
        {
            current.PriceIncVat = decimal.Round(Math.Max(0m, price), 2, MidpointRounding.AwayFromZero);
        }

        ManualServicePricingByJobId[jobId] = current;
        Recalc();
    }

    private bool ValidateManualServicePricingInputs(out string message)
    {
        SyncManualServicePricingInputs();
        var invalidCount = SelectedManualPricingJobs.Count(job => IsManualServiceInputInvalid(job.Id));
        if (invalidCount == 0)
        {
            message = "";
            return true;
        }

        message = $"Set minutes and price for {invalidCount} TBC service(s) before continuing.";
        return false;
    }

    private IReadOnlyDictionary<string, List<JobDefinition>> JobGroups
    {
        get
        {
            var orderedJobs = Catalog
                .OrderJobsForDisplay(
                    Catalog.Jobs.Where(j => string.IsNullOrWhiteSpace(JobFilter)
                                            || j.Name.Contains(JobFilter, StringComparison.OrdinalIgnoreCase)),
                    servicePackagesFirst: true)
                .ToList();

            return orderedJobs
                .GroupBy(j => (j.Category ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToList(),
                    StringComparer.OrdinalIgnoreCase);
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

        var service = Catalog.Jobs.FirstOrDefault(j => j.Id.Equals(serviceId, StringComparison.OrdinalIgnoreCase));
        if (service is not null && RequiresManualPricingAtUse(service))
            return "TBC - set in booking";

        var (mins, price, _) = Catalog.PriceAndTime(serviceId, SelectedPackageJobId);
        return $"{mins} mins / GBP {price:0.00}";
    }

    private string GetJobMeta(JobDefinition job)
    {
        if (RequiresManualPricingAtUse(job))
            return "TBC - set in booking";

        return $"{Math.Max(0, job.DefaultMinutes)} mins / GBP {Math.Max(0m, job.BasePriceIncVat):0.00}";
    }

    private IReadOnlyDictionary<string, List<JobDefinition>> PackageServiceGroups
    {
        get
        {
            if (!CanSelectPackageServices)
                return new Dictionary<string, List<JobDefinition>>(StringComparer.OrdinalIgnoreCase);

            var packageId = SelectedPackageJobId;
            var orderedJobs = Catalog
                .OrderJobsForDisplay(
                    Catalog.Jobs
                        .Where(j => !IsServicePackage(j.Id))
                        .Where(j => Catalog.IsServiceAvailableAsAdditionalService(j.Id, packageId))
                        .Where(j => string.IsNullOrWhiteSpace(PackageServiceFilter)
                                    || j.Name.Contains(PackageServiceFilter, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            return orderedJobs
                .GroupBy(j => (j.Category ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(j => j.Name, StringComparer.OrdinalIgnoreCase).ToList(),
                    StringComparer.OrdinalIgnoreCase);
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

        SyncManualServicePricingInputs();
    }

    private void ApplyManualPricingFromQuery(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return;

        foreach (var token in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = token.Split(':', StringSplitOptions.TrimEntries);
            if (parts.Length < 3)
                continue;

            var jobId = (parts[0] ?? "").Trim();
            if (string.IsNullOrWhiteSpace(jobId))
                continue;

            if (!ManualServicePricingByJobId.ContainsKey(jobId))
            {
                var job = Catalog.Jobs.FirstOrDefault(j => j.Id.Equals(jobId, StringComparison.OrdinalIgnoreCase));
                if (job is null || !RequiresManualPricingAtUse(job))
                    continue;

                ManualServicePricingByJobId[jobId] = new ManualServicePricingInput { JobId = jobId };
            }

            if (int.TryParse(parts[1], out var minutes))
                ManualServicePricingByJobId[jobId].Minutes = Math.Max(0, minutes);

            if (decimal.TryParse(parts[2], out var price)
                || decimal.TryParse(parts[2], NumberStyles.Number, CultureInfo.InvariantCulture, out price))
            {
                ManualServicePricingByJobId[jobId].PriceIncVat = decimal.Round(Math.Max(0m, price), 2, MidpointRounding.AwayFromZero);
            }
        }

        Recalc();
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

    private sealed class ManualServicePricingInput
    {
        public string JobId { get; set; } = "";
        public int Minutes { get; set; }
        public decimal PriceIncVat { get; set; }
    }
}

