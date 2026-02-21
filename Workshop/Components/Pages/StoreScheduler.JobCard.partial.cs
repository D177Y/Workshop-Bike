using Microsoft.AspNetCore.Components;
using Workshop.Models;
using Workshop.Services;

namespace Workshop.Components.Pages;

public partial class StoreScheduler
{
    [Inject] private StoreSchedulerBookingProjectionService BookingProjection { get; set; } = default!;

    private bool IsJobCardDialogVisible;
    private int? JobCardBookingId;
    private BookingJobCard? JobCardEditor;
    private string JobCardMessage = "";
    private bool IsJobCardAmendMode;
    private bool IsJobCardOverrunDialogVisible;
    private string JobCardOverrunMessage = "";

    private string JobCardServiceFilter = "";
    private readonly HashSet<string> JobCardExpandedGroups = new(StringComparer.OrdinalIgnoreCase);
    private string JobCardAdditionalServiceFilter = "";
    private readonly HashSet<string> JobCardExpandedAdditionalGroups = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> ExpandedPackageChecklistRows = new(StringComparer.OrdinalIgnoreCase);

    private int JobCardOriginalMinutes;
    private decimal JobCardOriginalTotalPrice;
    private string JobCardOriginalServiceSnapshot = "";
    private string JobCardOriginalServiceSummary = "";
    private List<JobCardServiceItem> JobCardOriginalServices = new();

    private string CustomServiceName = "";
    private int CustomServiceMinutes = 30;
    private decimal CustomServicePrice;

    private string PartSearch = "";
    private List<PartLookupItem> FilteredPartCatalog => SearchParts(PartSearch);

    private static readonly List<PartLookupItem> LocalPartCatalog = new()
    {
        new PartLookupItem("P-CHAIN-11", "11 speed chain", 28.50m),
        new PartLookupItem("P-PAD-DISC", "Disc brake pad set", 14.95m),
        new PartLookupItem("P-TYRE-700", "700c all weather tyre", 32.00m),
        new PartLookupItem("P-TUBE-700", "700c inner tube", 6.50m),
        new PartLookupItem("P-CABLE-GEAR", "Gear cable kit", 11.20m),
        new PartLookupItem("P-CABLE-BRAKE", "Brake cable kit", 10.80m),
        new PartLookupItem("P-BB-SEALED", "Sealed bottom bracket", 24.00m),
        new PartLookupItem("P-CASS-11", "11 speed cassette", 56.00m)
    };

    private string BuildCustomerName(Booking booking)
        => BookingProjection.BuildCustomerName(booking);

    private List<string> ResolveBookingJobIds(Booking booking)
        => BookingProjection.ResolveBookingJobIds(booking);

    private string? ResolveServicePackageId(Booking booking)
        => BookingProjection.ResolveServicePackageId(booking);

    private string JobCardDialogTitle
    {
        get
        {
            if (!JobCardBookingId.HasValue)
                return "Job Card";

            var booking = Data.Bookings.FirstOrDefault(b => b.Id == JobCardBookingId.Value);
            if (booking is null)
                return $"Job Card #{JobCardBookingId.Value}";

            var customer = BuildCustomerName(booking);
            return $"Job Card #{booking.Id} - {customer}";
        }
    }

    private string GetJobCardActionLabel(int bookingId)
    {
        var booking = Data.Bookings.FirstOrDefault(b => b.Id == bookingId);
        if (booking?.JobCard is null)
            return "Start Job";
        return "Open Job Card";
    }

    private async Task OpenJobCard(int bookingId)
    {
        var booking = Data.Bookings.FirstOrDefault(b => b.Id == bookingId && b.StoreId == StoreId);
        if (booking is null)
            return;

        var created = false;
        if (booking.JobCard is null)
        {
            booking.JobCard = BuildInitialJobCard(booking);
            created = true;

            if (Data.Statuses.Any(s => s.Name.Equals("In Progress", StringComparison.OrdinalIgnoreCase)))
            {
                booking.StatusName = "In Progress";
                booking.JobCard.StatusName = "In Progress";
            }

            await Data.UpdateBookingAsync(booking);
            ReloadAppointmentsFromBookings();
            _ = ScheduleRef?.RefreshEventsAsync();
        }

        JobCardBookingId = bookingId;
        JobCardEditor = CloneJobCard(booking.JobCard ?? BuildInitialJobCard(booking));
        if (JobCardEditor.AssignedMechanicId == 0)
            JobCardEditor.AssignedMechanicId = booking.MechanicId;

        NormalizeJobCardServiceRules();

        JobCardOriginalMinutes = GetJobCardEstimatedMinutes(JobCardEditor);
        if (JobCardOriginalMinutes <= 0)
            JobCardOriginalMinutes = GetBookingDurationMinutes(booking);

        JobCardOriginalTotalPrice = GetJobCardServicesTotal(JobCardEditor);
        if (JobCardOriginalTotalPrice <= 0)
            JobCardOriginalTotalPrice = Math.Max(0, booking.TotalPriceIncVat);
        JobCardOriginalServiceSnapshot = BuildServiceAmendmentSnapshot(JobCardEditor.Services);
        JobCardOriginalServiceSummary = DescribeServicesForNote(JobCardEditor.Services);
        JobCardOriginalServices = CloneServiceItems(JobCardEditor.Services);

        JobCardMessage = created ? "Job card created. Track progress as work is completed." : "";
        IsJobCardAmendMode = false;
        JobCardServiceFilter = "";
        JobCardAdditionalServiceFilter = "";
        JobCardExpandedGroups.Clear();
        JobCardExpandedAdditionalGroups.Clear();
        ExpandedPackageChecklistRows.Clear();
        IsJobCardOverrunDialogVisible = false;
        JobCardOverrunMessage = "";
        CustomServiceName = "";
        CustomServiceMinutes = 30;
        CustomServicePrice = 0;
        PartSearch = "";

        IsJobCardDialogVisible = true;

        if (ScheduleRef is not null)
            await ScheduleRef.CloseQuickInfoPopupAsync();
    }

    private void CloseJobCard()
    {
        IsJobCardDialogVisible = false;
        JobCardBookingId = null;
        JobCardEditor = null;
        JobCardMessage = "";
        IsJobCardAmendMode = false;
        IsJobCardOverrunDialogVisible = false;
        JobCardOverrunMessage = "";
        JobCardServiceFilter = "";
        JobCardAdditionalServiceFilter = "";
        JobCardExpandedGroups.Clear();
        JobCardExpandedAdditionalGroups.Clear();
        ExpandedPackageChecklistRows.Clear();
        JobCardOriginalMinutes = 0;
        JobCardOriginalTotalPrice = 0m;
        JobCardOriginalServiceSnapshot = "";
        JobCardOriginalServiceSummary = "";
        JobCardOriginalServices = new List<JobCardServiceItem>();
        CustomServiceName = "";
        PartSearch = "";
    }

    private BookingJobCard BuildInitialJobCard(Booking booking)
    {
        var card = new BookingJobCard
        {
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedUtc = DateTime.UtcNow,
            AssignedMechanicId = booking.MechanicId,
            StatusName = string.IsNullOrWhiteSpace(booking.StatusName) ? "Scheduled" : booking.StatusName.Trim(),
            CustomerNotes = (booking.Notes ?? "").Trim(),
            CommunicationDraft = $"Update on your booking ({booking.Title})."
        };

        var selectedJobIds = ResolveBookingJobIds(booking);

        var servicePackageId = ResolveServicePackageId(booking);

        foreach (var jobId in selectedJobIds.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var job = Catalog.Jobs.FirstOrDefault(j => j.Id.Equals(jobId, StringComparison.OrdinalIgnoreCase));
            if (job is null)
                continue;

            var packageContext = Catalog.IsServicePackage(job.Id) ? null : servicePackageId;
            var (mins, price, _) = Catalog.PriceAndTime(job.Id, packageContext);
            var serviceItem = new JobCardServiceItem
            {
                JobId = job.Id,
                Name = job.Name,
                Description = (job.Description ?? "").Trim(),
                EstimatedMinutes = mins,
                EstimatedPriceIncVat = price
            };
            card.Services.Add(serviceItem);
            if (Catalog.IsServicePackage(job.Id))
                AppendPackageChecklistItems(card, serviceItem);
        }

        if (card.Services.Count == 0)
        {
            card.Services.Add(new JobCardServiceItem
            {
                Name = string.IsNullOrWhiteSpace(booking.Title) ? "Workshop service" : booking.Title.Trim(),
                EstimatedMinutes = Math.Max(0, booking.TotalMinutes),
                EstimatedPriceIncVat = Math.Max(0, booking.TotalPriceIncVat)
            });
        }

        return card;
    }

    private static BookingJobCard CloneJobCard(BookingJobCard source)
    {
        return new BookingJobCard
        {
            StartedAtUtc = source.StartedAtUtc,
            LastUpdatedUtc = source.LastUpdatedUtc,
            AssignedMechanicId = source.AssignedMechanicId,
            StatusName = source.StatusName,
            ServiceNotes = source.ServiceNotes,
            CustomerNotes = source.CustomerNotes,
            CommunicationDraft = source.CommunicationDraft,
            LastSmsSentUtc = source.LastSmsSentUtc,
            LastEmailSentUtc = source.LastEmailSentUtc,
            Services = source.Services.Select(s => new JobCardServiceItem
            {
                Id = s.Id,
                JobId = s.JobId,
                Name = s.Name,
                Description = s.Description,
                EstimatedMinutes = s.EstimatedMinutes,
                EstimatedPriceIncVat = s.EstimatedPriceIncVat,
                IsCompleted = s.IsCompleted,
                IsPackageChecklistItem = s.IsPackageChecklistItem,
                ParentPackageServiceItemId = s.ParentPackageServiceItemId,
                ChecklistTemplateItemId = s.ChecklistTemplateItemId,
                ChecklistSourceJobId = s.ChecklistSourceJobId,
                ChecklistSortOrder = s.ChecklistSortOrder
            }).ToList(),
            Parts = source.Parts.Select(p => new JobCardPartItem
            {
                Id = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                UnitPriceIncVat = p.UnitPriceIncVat,
                Quantity = p.Quantity,
                IsFitted = p.IsFitted
            }).ToList(),
            MessageLog = source.MessageLog.Select(m => new JobCardMessageLogItem
            {
                SentAtUtc = m.SentAtUtc,
                Channel = m.Channel,
                Recipient = m.Recipient,
                Summary = m.Summary
            }).ToList()
        };
    }

    private static List<JobCardServiceItem> CloneServiceItems(IEnumerable<JobCardServiceItem> services)
        => services.Select(s => new JobCardServiceItem
        {
            Id = s.Id,
            JobId = s.JobId,
            Name = s.Name,
            Description = s.Description,
            EstimatedMinutes = s.EstimatedMinutes,
            EstimatedPriceIncVat = s.EstimatedPriceIncVat,
            IsCompleted = s.IsCompleted,
            IsPackageChecklistItem = s.IsPackageChecklistItem,
            ParentPackageServiceItemId = s.ParentPackageServiceItemId,
            ChecklistTemplateItemId = s.ChecklistTemplateItemId,
            ChecklistSourceJobId = s.ChecklistSourceJobId,
            ChecklistSortOrder = s.ChecklistSortOrder
        }).ToList();

    private string? SelectedJobCardPackageId =>
        JobCardEditor?.Services
            .Select(s => (s.JobId ?? "").Trim())
            .FirstOrDefault(jobId => !string.IsNullOrWhiteSpace(jobId) && Catalog.IsServicePackage(jobId));

    private bool CanSelectJobCardPackageServices => !string.IsNullOrWhiteSpace(SelectedJobCardPackageId);

    private static bool IsPackageChecklistRow(JobCardServiceItem service)
    {
        if (service.IsPackageChecklistItem)
            return true;

        return !string.IsNullOrWhiteSpace((service.ParentPackageServiceItemId ?? "").Trim());
    }

    private bool IsServicePackageRow(JobCardServiceItem service)
    {
        if (IsPackageChecklistRow(service))
            return false;

        var jobId = (service.JobId ?? "").Trim();
        return !string.IsNullOrWhiteSpace(jobId) && Catalog.IsServicePackage(jobId);
    }

    private IReadOnlyList<JobCardServiceItem> GetJobCardTopLevelServices(BookingJobCard card)
        => card.Services
            .Where(s => !IsPackageChecklistRow(s))
            .ToList();

    private IReadOnlyList<JobCardServiceItem> GetJobCardPackageChecklistServices(string parentServiceItemId)
    {
        if (JobCardEditor is null || string.IsNullOrWhiteSpace(parentServiceItemId))
            return new List<JobCardServiceItem>();

        return JobCardEditor.Services
            .Where(s => IsPackageChecklistRow(s)
                        && (s.ParentPackageServiceItemId ?? "").Trim().Equals(parentServiceItemId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s.ChecklistSortOrder)
            .ThenBy(s => (s.Name ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private bool IsJobCardServicePackageWithChecklist(JobCardServiceItem service)
        => IsServicePackageRow(service) && GetJobCardPackageChecklistServices(service.Id).Count > 0;

    private bool IsPackageChecklistExpanded(string parentServiceItemId)
        => !string.IsNullOrWhiteSpace(parentServiceItemId)
           && ExpandedPackageChecklistRows.Contains(parentServiceItemId);

    private void TogglePackageChecklist(string parentServiceItemId)
    {
        if (string.IsNullOrWhiteSpace(parentServiceItemId))
            return;

        if (!ExpandedPackageChecklistRows.Add(parentServiceItemId))
            ExpandedPackageChecklistRows.Remove(parentServiceItemId);
    }

    private void ToggleJobCardServiceCompleted(JobCardServiceItem service, ChangeEventArgs e)
    {
        if (JobCardEditor is null)
            return;

        var isCompleted = e?.Value is bool checkedValue && checkedValue;
        service.IsCompleted = isCompleted;
        if (!IsServicePackageRow(service))
            return;

        foreach (var checklistItem in GetJobCardPackageChecklistServices(service.Id))
            checklistItem.IsCompleted = isCompleted;
    }

    private IReadOnlyDictionary<string, List<JobDefinition>> JobCardServiceGroups
    {
        get
        {
            var filtered = Catalog.Jobs
                .Where(j => string.IsNullOrWhiteSpace(JobCardServiceFilter) || j.Name.Contains(JobCardServiceFilter, StringComparison.OrdinalIgnoreCase))
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

    private IReadOnlyDictionary<string, List<JobDefinition>> JobCardAdditionalServiceGroups
    {
        get
        {
            if (!CanSelectJobCardPackageServices)
                return new Dictionary<string, List<JobDefinition>>(StringComparer.OrdinalIgnoreCase);

            var packageId = SelectedJobCardPackageId;
            return Catalog.Jobs
                .Where(j => !Catalog.IsServicePackage(j.Id))
                .Where(j => Catalog.IsServiceAvailableAsAdditionalService(j.Id, packageId))
                .Where(j => string.IsNullOrWhiteSpace(JobCardAdditionalServiceFilter) || j.Name.Contains(JobCardAdditionalServiceFilter, StringComparison.OrdinalIgnoreCase))
                .GroupBy(j => j.Category)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.OrderBy(j => j.Name).ToList(), StringComparer.OrdinalIgnoreCase);
        }
    }

    private bool IsJobCardGroupOpen(string key)
    {
        if (!string.IsNullOrWhiteSpace(JobCardServiceFilter))
            return true;

        if (JobCardExpandedGroups.Count == 0)
            return key.Equals("Service Packages", StringComparison.OrdinalIgnoreCase);

        return JobCardExpandedGroups.Contains(key);
    }

    private void ToggleJobCardGroup(string key)
    {
        if (JobCardExpandedGroups.Count == 0)
            JobCardExpandedGroups.Add("Service Packages");

        if (JobCardExpandedGroups.Contains(key))
            JobCardExpandedGroups.Remove(key);
        else
            JobCardExpandedGroups.Add(key);
    }

    private bool IsJobCardAdditionalGroupOpen(string key)
    {
        if (!string.IsNullOrWhiteSpace(JobCardAdditionalServiceFilter))
            return true;

        if (JobCardExpandedAdditionalGroups.Count == 0)
        {
            var first = JobCardAdditionalServiceGroups.Keys.FirstOrDefault();
            return first != null && first.Equals(key, StringComparison.OrdinalIgnoreCase);
        }

        return JobCardExpandedAdditionalGroups.Contains(key);
    }

    private void ToggleJobCardAdditionalGroup(string key)
    {
        if (JobCardExpandedAdditionalGroups.Count == 0)
        {
            var first = JobCardAdditionalServiceGroups.Keys.FirstOrDefault();
            if (first is not null)
                JobCardExpandedAdditionalGroups.Add(first);
        }

        if (JobCardExpandedAdditionalGroups.Contains(key))
            JobCardExpandedAdditionalGroups.Remove(key);
        else
            JobCardExpandedAdditionalGroups.Add(key);
    }

    private void ToggleJobCardAmendMode()
    {
        IsJobCardAmendMode = !IsJobCardAmendMode;
        if (!IsJobCardAmendMode)
        {
            JobCardServiceFilter = "";
            JobCardAdditionalServiceFilter = "";
        }
    }

    private void ResetJobCardServicesToOriginal()
    {
        if (JobCardEditor is null || JobCardOriginalServices.Count == 0)
            return;

        JobCardEditor.Services = CloneServiceItems(JobCardOriginalServices);
        NormalizeJobCardServiceRules();
        JobCardMessage = "Services reset to booked-in values.";
    }

    private bool IsJobCardPrimaryServiceSelected(string jobId)
    {
        if (JobCardEditor is null || string.IsNullOrWhiteSpace(jobId))
            return false;

        var exists = JobCardEditor.Services.Any(s => s.JobId.Equals(jobId, StringComparison.OrdinalIgnoreCase));
        if (Catalog.IsServicePackage(jobId))
            return exists;

        return !CanSelectJobCardPackageServices && exists;
    }

    private bool IsJobCardAdditionalServiceSelected(string serviceId)
    {
        if (JobCardEditor is null || string.IsNullOrWhiteSpace(serviceId) || !CanSelectJobCardPackageServices)
            return false;

        return JobCardEditor.Services.Any(s => s.JobId.Equals(serviceId, StringComparison.OrdinalIgnoreCase));
    }

    private string GetJobCardPackageServiceMeta(string serviceId)
    {
        if (!CanSelectJobCardPackageServices || string.IsNullOrWhiteSpace(SelectedJobCardPackageId))
            return "";

        var (mins, price, _) = Catalog.PriceAndTime(serviceId, SelectedJobCardPackageId);
        return $"{mins} mins · £{price:0.00}";
    }

    private string ResolveChecklistItemName(ServicePackageChecklistItemDefinition definition)
    {
        var linkedServiceJobId = (definition.LinkedServiceJobId ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(linkedServiceJobId))
        {
            var linkedService = Catalog.Jobs.FirstOrDefault(j => j.Id.Equals(linkedServiceJobId, StringComparison.OrdinalIgnoreCase));
            if (linkedService is not null)
                return (linkedService.Name ?? "").Trim();
        }

        return (definition.Name ?? "").Trim();
    }

    private string ResolveChecklistItemDescription(ServicePackageChecklistItemDefinition definition)
    {
        var linkedServiceJobId = (definition.LinkedServiceJobId ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(linkedServiceJobId))
        {
            var linkedService = Catalog.Jobs.FirstOrDefault(j => j.Id.Equals(linkedServiceJobId, StringComparison.OrdinalIgnoreCase));
            if (linkedService is not null)
                return (linkedService.Description ?? "").Trim();
        }

        return (definition.Description ?? "").Trim();
    }

    private void AppendPackageChecklistItems(BookingJobCard card, JobCardServiceItem packageRow, bool onlyIfMissing = true)
    {
        if (!IsServicePackageRow(packageRow))
            return;

        var parentId = (packageRow.Id ?? "").Trim();
        if (string.IsNullOrWhiteSpace(parentId))
            return;

        if (onlyIfMissing && card.Services.Any(s =>
            IsPackageChecklistRow(s)
            && (s.ParentPackageServiceItemId ?? "").Trim().Equals(parentId, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var packageJobId = (packageRow.JobId ?? "").Trim();
        if (string.IsNullOrWhiteSpace(packageJobId))
            return;

        var packageJob = Catalog.Jobs.FirstOrDefault(j => j.Id.Equals(packageJobId, StringComparison.OrdinalIgnoreCase));
        if (packageJob is null || packageJob.PackageChecklistItems.Count == 0)
            return;

        var ordered = packageJob.PackageChecklistItems
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => (i.Name ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
            .ToList();

        var insertIndex = card.Services.FindIndex(s => s.Id.Equals(parentId, StringComparison.OrdinalIgnoreCase));
        if (insertIndex < 0)
            insertIndex = card.Services.Count - 1;

        var rowIndex = 0;
        foreach (var definition in ordered)
        {
            var name = ResolveChecklistItemName(definition);
            var description = ResolveChecklistItemDescription(definition);
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var checklistItem = new JobCardServiceItem
            {
                Name = name,
                Description = description,
                IsCompleted = packageRow.IsCompleted,
                IsPackageChecklistItem = true,
                ParentPackageServiceItemId = parentId,
                ChecklistTemplateItemId = (definition.Id ?? "").Trim(),
                ChecklistSourceJobId = (definition.LinkedServiceJobId ?? "").Trim(),
                ChecklistSortOrder = rowIndex,
                EstimatedMinutes = 0,
                EstimatedPriceIncVat = 0m
            };

            insertIndex++;
            if (insertIndex >= card.Services.Count)
                card.Services.Add(checklistItem);
            else
                card.Services.Insert(insertIndex, checklistItem);

            rowIndex++;
        }
    }

    private void ToggleJobCardService(JobDefinition job, ChangeEventArgs e)
    {
        if (JobCardEditor is null)
            return;

        var isChecked = e?.Value is bool b && b;
        var isPackage = Catalog.IsServicePackage(job.Id);
        var hasPackage = CanSelectJobCardPackageServices;

        if (isPackage)
        {
            if (isChecked)
            {
                RemoveAllCatalogServicesFromJobCard();
                AddCatalogServiceToJobCard(job.Id, null);
            }
            else
            {
                RemoveAllCatalogServicesFromJobCard();
            }
        }
        else
        {
            if (hasPackage)
                RemoveAllCatalogServicesFromJobCard();

            if (isChecked)
                AddCatalogServiceToJobCard(job.Id, null);
            else
                RemoveCatalogServiceFromJobCard(job.Id);
        }

        NormalizeJobCardServiceRules();
    }

    private void ToggleJobCardAdditionalService(string serviceId, ChangeEventArgs e)
    {
        if (JobCardEditor is null || string.IsNullOrWhiteSpace(serviceId) || string.IsNullOrWhiteSpace(SelectedJobCardPackageId))
            return;

        if (!Catalog.IsServiceAvailableAsAdditionalService(serviceId, SelectedJobCardPackageId))
        {
            RemoveCatalogServiceFromJobCard(serviceId);
            return;
        }

        var isChecked = e?.Value is bool b && b;
        if (isChecked)
            AddCatalogServiceToJobCard(serviceId, SelectedJobCardPackageId);
        else
            RemoveCatalogServiceFromJobCard(serviceId);

        NormalizeJobCardServiceRules();
    }

    private void AddCatalogServiceToJobCard(string jobId, string? packageContext)
    {
        if (JobCardEditor is null || string.IsNullOrWhiteSpace(jobId))
            return;

        if (JobCardEditor.Services.Any(s => !IsPackageChecklistRow(s) && s.JobId.Equals(jobId, StringComparison.OrdinalIgnoreCase)))
            return;

        var job = Catalog.Jobs.FirstOrDefault(j => j.Id.Equals(jobId, StringComparison.OrdinalIgnoreCase));
        if (job is null)
            return;

        var resolvedContext = Catalog.IsServicePackage(job.Id) ? null : packageContext;
        var (mins, price, _) = Catalog.PriceAndTime(job.Id, resolvedContext);
        var serviceRow = new JobCardServiceItem
        {
            JobId = job.Id,
            Name = job.Name,
            Description = (job.Description ?? "").Trim(),
            EstimatedMinutes = mins,
            EstimatedPriceIncVat = price
        };
        JobCardEditor.Services.Add(serviceRow);

        if (Catalog.IsServicePackage(job.Id))
            AppendPackageChecklistItems(JobCardEditor, serviceRow);
    }

    private void RemoveCatalogServiceFromJobCard(string jobId)
    {
        if (JobCardEditor is null || string.IsNullOrWhiteSpace(jobId))
            return;

        var removedPackageParentIds = JobCardEditor.Services
            .Where(s => !IsPackageChecklistRow(s)
                        && s.JobId.Equals(jobId, StringComparison.OrdinalIgnoreCase)
                        && Catalog.IsServicePackage(s.JobId))
            .Select(s => s.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        JobCardEditor.Services.RemoveAll(s => !IsPackageChecklistRow(s) && s.JobId.Equals(jobId, StringComparison.OrdinalIgnoreCase));
        if (removedPackageParentIds.Count == 0)
            return;

        JobCardEditor.Services.RemoveAll(s =>
            IsPackageChecklistRow(s)
            && removedPackageParentIds.Contains((s.ParentPackageServiceItemId ?? "").Trim()));
        ExpandedPackageChecklistRows.RemoveWhere(id => removedPackageParentIds.Contains(id));
    }

    private void RemoveAllCatalogServicesFromJobCard()
    {
        if (JobCardEditor is null)
            return;

        JobCardEditor.Services.RemoveAll(s =>
            IsPackageChecklistRow(s)
            || !string.IsNullOrWhiteSpace((s.JobId ?? "").Trim()));
        ExpandedPackageChecklistRows.Clear();
    }

    private void NormalizeJobCardServiceRules()
    {
        if (JobCardEditor is null)
            return;

        var validCatalogIds = Catalog.Jobs
            .Select(j => j.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var service in JobCardEditor.Services)
        {
            var id = (service.JobId ?? "").Trim();
            if (string.IsNullOrWhiteSpace(id) || validCatalogIds.Contains(id))
                continue;

            service.JobId = "";
        }

        JobCardEditor.Services.RemoveAll(s =>
            IsPackageChecklistRow(s)
            && string.IsNullOrWhiteSpace((s.ParentPackageServiceItemId ?? "").Trim()));

        var packageRows = JobCardEditor.Services
            .Where(IsServicePackageRow)
            .ToList();
        if (packageRows.Count > 1)
        {
            var keepId = packageRows[0].Id;
            var removedIds = packageRows
                .Skip(1)
                .Select(s => (s.Id ?? "").Trim())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            JobCardEditor.Services.RemoveAll(s =>
                (IsServicePackageRow(s) && !s.Id.Equals(keepId, StringComparison.OrdinalIgnoreCase))
                || (IsPackageChecklistRow(s) && removedIds.Contains((s.ParentPackageServiceItemId ?? "").Trim())));

            packageRows = JobCardEditor.Services
                .Where(IsServicePackageRow)
                .ToList();
        }

        var packageRow = packageRows.FirstOrDefault();
        if (packageRow is null)
        {
            JobCardEditor.Services.RemoveAll(IsPackageChecklistRow);
            ExpandedPackageChecklistRows.Clear();
            return;
        }

        var packageId = (packageRow.JobId ?? "").Trim();
        if (string.IsNullOrWhiteSpace(packageId))
            return;

        JobCardEditor.Services.RemoveAll(s =>
        {
            var serviceJobId = (s.JobId ?? "").Trim();
            return !IsPackageChecklistRow(s)
                   && !string.IsNullOrWhiteSpace(serviceJobId)
                   && !Catalog.IsServicePackage(serviceJobId)
                   && !Catalog.IsServiceAvailableAsAdditionalService(serviceJobId, packageId);
        });

        var packageParentId = (packageRow.Id ?? "").Trim();
        JobCardEditor.Services.RemoveAll(s =>
            IsPackageChecklistRow(s)
            && !(s.ParentPackageServiceItemId ?? "").Trim().Equals(packageParentId, StringComparison.OrdinalIgnoreCase));

        AppendPackageChecklistItems(JobCardEditor, packageRow, onlyIfMissing: true);

        ExpandedPackageChecklistRows.RemoveWhere(id =>
            !JobCardEditor.Services.Any(s => IsServicePackageRow(s) && s.Id.Equals(id, StringComparison.OrdinalIgnoreCase)));
    }

    private void AddCustomService()
    {
        if (JobCardEditor is null)
            return;

        var name = (CustomServiceName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
            return;

        JobCardEditor.Services.Add(new JobCardServiceItem
        {
            Name = name,
            EstimatedMinutes = Math.Max(0, CustomServiceMinutes),
            EstimatedPriceIncVat = Math.Max(0, CustomServicePrice)
        });

        CustomServiceName = "";
        CustomServiceMinutes = 30;
        CustomServicePrice = 0;
    }

    private void RemoveService(string serviceId)
    {
        if (JobCardEditor is null || string.IsNullOrWhiteSpace(serviceId))
            return;

        JobCardEditor.Services.RemoveAll(s => s.Id == serviceId);
        NormalizeJobCardServiceRules();
    }

    private List<PartLookupItem> SearchParts(string term)
    {
        var normalized = (term ?? "").Trim();
        if (normalized.Length < 2)
            return new List<PartLookupItem>();

        return LocalPartCatalog
            .Where(p =>
                p.Name.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                p.Sku.Contains(normalized, StringComparison.OrdinalIgnoreCase))
            .Take(8)
            .ToList();
    }

    private void AddBlankPart()
    {
        if (JobCardEditor is null)
            return;

        JobCardEditor.Parts.Add(new JobCardPartItem
        {
            Name = "New part",
            Quantity = 1
        });
    }

    private void AddPartFromLookup(PartLookupItem item)
    {
        if (JobCardEditor is null)
            return;

        JobCardEditor.Parts.Add(new JobCardPartItem
        {
            Sku = item.Sku,
            Name = item.Name,
            UnitPriceIncVat = item.UnitPriceIncVat,
            Quantity = 1
        });

        PartSearch = "";
    }

    private void RemovePart(string partId)
    {
        if (JobCardEditor is null || string.IsNullOrWhiteSpace(partId))
            return;

        JobCardEditor.Parts.RemoveAll(p => p.Id == partId);
    }

    private string GetJobCardServiceDescription(JobCardServiceItem service)
    {
        var stored = (service.Description ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(stored))
            return stored;

        var jobId = (service.JobId ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(jobId))
        {
            var linked = Catalog.Jobs.FirstOrDefault(j => j.Id.Equals(jobId, StringComparison.OrdinalIgnoreCase));
            if (linked is not null)
            {
                var linkedDescription = (linked.Description ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(linkedDescription))
                    return linkedDescription;
            }
        }

        var checklistSourceJobId = (service.ChecklistSourceJobId ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(checklistSourceJobId))
        {
            var linkedChecklistService = Catalog.Jobs.FirstOrDefault(j => j.Id.Equals(checklistSourceJobId, StringComparison.OrdinalIgnoreCase));
            if (linkedChecklistService is not null)
            {
                var linkedDescription = (linkedChecklistService.Description ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(linkedDescription))
                    return linkedDescription;
            }
        }

        return "-";
    }

    private IEnumerable<JobCardServiceItem> GetProgressTrackedServices(BookingJobCard card)
        => card.Services.Where(s => !IsPackageChecklistRow(s));

    private int GetJobCardEstimatedMinutes(BookingJobCard card)
        => GetProgressTrackedServices(card).Sum(s => Math.Max(0, s.EstimatedMinutes));

    private int GetJobCardCompletedMinutes(BookingJobCard card)
        => GetProgressTrackedServices(card).Where(s => s.IsCompleted).Sum(s => Math.Max(0, s.EstimatedMinutes));

    private decimal GetJobCardProgressPercent(BookingJobCard card)
    {
        var total = GetJobCardEstimatedMinutes(card);
        if (total <= 0)
            return 0m;

        var done = GetJobCardCompletedMinutes(card);
        return Math.Min(100m, Math.Round((decimal)done * 100m / total, 1, MidpointRounding.AwayFromZero));
    }

    private string GetJobCardProgress(BookingJobCard card)
    {
        var total = GetJobCardEstimatedMinutes(card);
        var done = GetJobCardCompletedMinutes(card);
        return $"{done}/{total} mins";
    }

    private string GetJobCardProgressFillStyle(BookingJobCard card)
        => $"width:{GetJobCardProgressPercent(card):0.#}%";

    private string GetJobCardProgressMinutesLabel(BookingJobCard card)
    {
        var tracked = GetProgressTrackedServices(card).ToList();
        var doneItems = tracked.Count(s => s.IsCompleted);
        var totalItems = tracked.Count;
        return $"{doneItems}/{totalItems} tasks complete · {GetJobCardProgressPercent(card):0.#}%";
    }

    private decimal GetJobCardServicesTotal(BookingJobCard card)
        => GetProgressTrackedServices(card).Sum(s => Math.Max(0, s.EstimatedPriceIncVat));

    private decimal GetJobCardPartsTotal(BookingJobCard card)
        => card.Parts.Sum(p => Math.Max(0, p.Quantity) * Math.Max(0, p.UnitPriceIncVat));

    private decimal GetJobCardEstimatedTotal(BookingJobCard card)
        => GetJobCardServicesTotal(card) + GetJobCardPartsTotal(card);

    private static string FormatJobCardTotals(int minutes, decimal price)
        => $"{minutes} mins · {price:C}";

    private bool HasJobCardTotalChanges(BookingJobCard card)
        => GetJobCardEstimatedMinutes(card) != JobCardOriginalMinutes
        || Math.Abs(GetJobCardServicesTotal(card) - JobCardOriginalTotalPrice) >= 0.01m;

    private string GetJobCardTotalsDeltaText(BookingJobCard card)
    {
        var minuteDelta = GetJobCardEstimatedMinutes(card) - JobCardOriginalMinutes;
        var priceDelta = GetJobCardServicesTotal(card) - JobCardOriginalTotalPrice;

        var minutePrefix = minuteDelta > 0 ? "+" : "";
        var pricePrefix = priceDelta > 0 ? "+" : "";
        return $"{minutePrefix}{minuteDelta} mins · {pricePrefix}{priceDelta:C}";
    }

    private string GetJobCardScheduleWindowText(BookingJobCard card)
    {
        if (!JobCardBookingId.HasValue)
            return "-";

        var booking = Data.Bookings.FirstOrDefault(b => b.Id == JobCardBookingId.Value && b.StoreId == StoreId);
        if (booking is null)
            return "-";

        var projectedEnd = GetProjectedBookingEnd(booking, card);
        return $"{FormatLocalDateTime(booking.Start)} - {FormatLocalDateTime(projectedEnd)}";
    }

    private static string BuildServiceAmendmentSnapshot(IEnumerable<JobCardServiceItem> services)
    {
        return string.Join("||",
            services
                .Where(s => !IsPackageChecklistRow(s))
                .Select(s =>
            {
                var jobId = (s.JobId ?? "").Trim();
                var name = (s.Name ?? "").Trim();
                var minutes = Math.Max(0, s.EstimatedMinutes);
                var price = Math.Round(Math.Max(0, s.EstimatedPriceIncVat), 2, MidpointRounding.AwayFromZero);
                return $"{jobId}|{name}|{minutes}|{price:0.00}";
            }));
    }

    private static string DescribeServicesForNote(IEnumerable<JobCardServiceItem> services)
    {
        var descriptions = services
            .Where(s => !IsPackageChecklistRow(s))
            .Where(s => !string.IsNullOrWhiteSpace(s.Name))
            .Select(s => $"{s.Name.Trim()} ({Math.Max(0, s.EstimatedMinutes)} mins, {Math.Max(0m, s.EstimatedPriceIncVat):C})")
            .ToList();

        return descriptions.Count == 0 ? "No services" : string.Join("; ", descriptions);
    }

    private static string BuildServiceAmendmentNote(string originalSummary, string updatedSummary)
    {
        var stamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        return $"[{stamp}] Work amended. Booked in: {originalSummary}. Updated: {updatedSummary}.";
    }

    private static void AppendServiceAmendmentNote(BookingJobCard card, string originalSummary, string updatedSummary)
    {
        var note = BuildServiceAmendmentNote(originalSummary, updatedSummary);
        card.ServiceNotes = string.IsNullOrWhiteSpace(card.ServiceNotes)
            ? note
            : $"{card.ServiceNotes}{Environment.NewLine}{note}";
    }

    private int GetProjectedBookingMinutes(Booking booking, BookingJobCard card)
    {
        var editedMinutes = GetJobCardEstimatedMinutes(card);
        if (editedMinutes > 0)
            return editedMinutes;

        var scheduledMinutes = GetBookingDurationMinutes(booking);
        if (scheduledMinutes > 0)
            return scheduledMinutes;

        return Math.Max(0, booking.TotalMinutes);
    }

    private DateTime GetProjectedBookingEnd(Booking booking, BookingJobCard card)
        => booking.Start.AddMinutes(GetProjectedBookingMinutes(booking, card));

    private static int GetBookingDurationMinutes(Booking booking)
    {
        var duration = booking.End - booking.Start;
        if (duration.TotalMinutes <= 0)
            return 0;

        return (int)Math.Round(duration.TotalMinutes, MidpointRounding.AwayFromZero);
    }

    private List<string> BuildJobCardOverrunWarnings(Booking booking, BookingJobCard card, DateTime projectedEnd)
    {
        var warnings = new List<string>();
        if (projectedEnd <= booking.End)
            return warnings;

        var store = Data.Stores.FirstOrDefault(s => s.Id == booking.StoreId);
        if (store is null || !store.TryGetHours(booking.Start, out _, out var storeCloseTime))
        {
            warnings.Add("The booking now extends into a period where the store is closed.");
        }
        else
        {
            var storeClose = booking.Start.Date.Add(storeCloseTime);
            if (projectedEnd > storeClose)
                warnings.Add($"Store closing is {storeClose:HH:mm}, and this runs over by {FormatDuration(projectedEnd - storeClose)}.");
        }

        var assignedMechanicId = card.AssignedMechanicId != 0 ? card.AssignedMechanicId : booking.MechanicId;
        if (assignedMechanicId != 0
            && Mechanics.Any(m => m.Id == assignedMechanicId)
            && Scheduler.WouldExceedDailyLimit(StoreId, assignedMechanicId, booking.Start, projectedEnd, booking.Id))
        {
            var mechanicName = Mechanics.FirstOrDefault(m => m.Id == assignedMechanicId)?.Name ?? "Selected mechanic";
            warnings.Add($"{mechanicName} would exceed max bookable hours for the day.");
        }

        return warnings;
    }

    private static string BuildJobCardOverrunMessage(IEnumerable<string> warnings)
    {
        var warningLines = warnings.Select(w => $"- {w}");
        return "This update extends the booking beyond normal limits.\n\n"
               + string.Join("\n", warningLines)
               + "\n\nDo you want to save anyway?";
    }

    private static string FormatDuration(TimeSpan overrun)
    {
        var totalMinutes = (int)Math.Ceiling(Math.Max(0, overrun.TotalMinutes));
        var hours = totalMinutes / 60;
        var minutes = totalMinutes % 60;
        if (hours > 0 && minutes > 0)
            return $"{hours}h {minutes}m";
        if (hours > 0)
            return $"{hours}h";
        return $"{minutes}m";
    }

    private static string FormatLocalDateTime(DateTime value)
    {
        var dateTime = value.Kind switch
        {
            DateTimeKind.Utc => value.ToLocalTime(),
            DateTimeKind.Local => value,
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc).ToLocalTime()
        };

        return dateTime.ToString("dd/MM/yyyy HH:mm");
    }

    private static string SummarizeMessage(string message)
    {
        var cleaned = (message ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
        if (cleaned.Length <= 80)
            return cleaned;
        return $"{cleaned[..77]}...";
    }

    private void NormalizeJobCardEditor()
    {
        if (JobCardEditor is null)
            return;

        JobCardEditor.StatusName = (JobCardEditor.StatusName ?? "").Trim();
        JobCardEditor.ServiceNotes = (JobCardEditor.ServiceNotes ?? "").Trim();
        JobCardEditor.CustomerNotes = (JobCardEditor.CustomerNotes ?? "").Trim();
        JobCardEditor.CommunicationDraft = (JobCardEditor.CommunicationDraft ?? "").Trim();

        foreach (var service in JobCardEditor.Services)
        {
            service.Name = (service.Name ?? "").Trim();
            service.Description = (service.Description ?? "").Trim();
            service.JobId = (service.JobId ?? "").Trim();
            service.ParentPackageServiceItemId = (service.ParentPackageServiceItemId ?? "").Trim();
            service.ChecklistTemplateItemId = (service.ChecklistTemplateItemId ?? "").Trim();
            service.ChecklistSourceJobId = (service.ChecklistSourceJobId ?? "").Trim();
            service.ChecklistSortOrder = Math.Max(0, service.ChecklistSortOrder);

            if (IsPackageChecklistRow(service))
            {
                service.IsPackageChecklistItem = true;
                service.JobId = "";
                service.EstimatedMinutes = 0;
                service.EstimatedPriceIncVat = 0m;
            }
            else
            {
                service.EstimatedMinutes = Math.Max(0, service.EstimatedMinutes);
                service.EstimatedPriceIncVat = Math.Max(0, service.EstimatedPriceIncVat);
            }
        }

        foreach (var part in JobCardEditor.Parts)
        {
            part.Name = (part.Name ?? "").Trim();
            part.Sku = (part.Sku ?? "").Trim();
            part.Quantity = Math.Max(0, part.Quantity);
            part.UnitPriceIncVat = Math.Max(0, part.UnitPriceIncVat);
        }

        JobCardEditor.Services = JobCardEditor.Services
            .Where(s => !string.IsNullOrWhiteSpace(s.Name))
            .ToList();

        JobCardEditor.Parts = JobCardEditor.Parts
            .Where(p => !string.IsNullOrWhiteSpace(p.Name) || !string.IsNullOrWhiteSpace(p.Sku))
            .ToList();

        NormalizeJobCardServiceRules();

        if (JobCardEditor.AssignedMechanicId == 0 && Mechanics.Count > 0)
            JobCardEditor.AssignedMechanicId = Mechanics[0].Id;

        if (string.IsNullOrWhiteSpace(JobCardEditor.StatusName))
            JobCardEditor.StatusName = "Scheduled";
    }

    private Task SaveJobCard()
        => PersistJobCardAsync(showSuccessMessage: true);

    private async Task PersistJobCardAsync(bool showSuccessMessage, bool skipOverrunWarnings = false)
    {
        if (!JobCardBookingId.HasValue || JobCardEditor is null)
            return;

        var booking = Data.Bookings.FirstOrDefault(b => b.Id == JobCardBookingId.Value && b.StoreId == StoreId);
        if (booking is null)
        {
            JobCardMessage = "Booking not found.";
            return;
        }

        NormalizeJobCardEditor();
        JobCardEditor.LastUpdatedUtc = DateTime.UtcNow;
        var currentServiceSnapshot = BuildServiceAmendmentSnapshot(JobCardEditor.Services);
        var currentServiceSummary = DescribeServicesForNote(JobCardEditor.Services);
        var hasServiceAmendment = !string.Equals(currentServiceSnapshot, JobCardOriginalServiceSnapshot, StringComparison.Ordinal);
        var projectedMinutes = GetProjectedBookingMinutes(booking, JobCardEditor);
        var projectedEnd = GetProjectedBookingEnd(booking, JobCardEditor);

        if (!skipOverrunWarnings)
        {
            var warnings = BuildJobCardOverrunWarnings(booking, JobCardEditor, projectedEnd);
            if (warnings.Count > 0)
            {
                JobCardOverrunMessage = BuildJobCardOverrunMessage(warnings);
                IsJobCardOverrunDialogVisible = true;
                return;
            }
        }

        if (JobCardEditor.AssignedMechanicId != 0 &&
            JobCardEditor.AssignedMechanicId != booking.MechanicId &&
            Scheduler.WouldExceedDailyLimit(StoreId, JobCardEditor.AssignedMechanicId, booking.Start, projectedEnd, booking.Id))
        {
            var mechanic = Mechanics.FirstOrDefault(m => m.Id == JobCardEditor.AssignedMechanicId);
            var name = mechanic?.Name ?? "That mechanic";
            JobCardMessage = $"{name} cannot take this transfer without exceeding daily limit.";
            return;
        }

        if (hasServiceAmendment)
            AppendServiceAmendmentNote(JobCardEditor, JobCardOriginalServiceSummary, currentServiceSummary);

        booking.JobCard = CloneJobCard(JobCardEditor);
        booking.StatusName = JobCardEditor.StatusName;
        if (JobCardEditor.AssignedMechanicId != 0)
            booking.MechanicId = JobCardEditor.AssignedMechanicId;
        booking.End = projectedEnd;

        var listedServices = JobCardEditor.Services
            .Where(s => !IsPackageChecklistRow(s))
            .Where(s => !string.IsNullOrWhiteSpace(s.Name))
            .ToList();

        if (listedServices.Count == 1)
            booking.Title = listedServices[0].Name.Trim();
        else if (listedServices.Count > 1)
            booking.Title = "Custom package";

        var validCatalogIds = JobCardEditor.Services
            .Where(s => !IsPackageChecklistRow(s))
            .Select(s => (s.JobId ?? "").Trim())
            .Where(id => !string.IsNullOrWhiteSpace(id) && Catalog.Jobs.Any(j => j.Id.Equals(id, StringComparison.OrdinalIgnoreCase)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (validCatalogIds.Length > 0)
        {
            booking.JobIds = validCatalogIds;
            booking.JobId = validCatalogIds[0];
        }
        else
        {
            booking.JobIds = Array.Empty<string>();
            booking.JobId = "MANUAL";
        }

        booking.TotalMinutes = projectedMinutes;

        booking.TotalPriceIncVat = GetJobCardEstimatedTotal(JobCardEditor);

        await Data.UpdateBookingAsync(booking);
        ReloadAppointmentsFromBookings();
        _ = ScheduleRef?.RefreshEventsAsync();

        IsJobCardOverrunDialogVisible = false;
        JobCardOverrunMessage = "";
        JobCardOriginalServiceSnapshot = BuildServiceAmendmentSnapshot(JobCardEditor.Services);
        JobCardOriginalServiceSummary = DescribeServicesForNote(JobCardEditor.Services);
        JobCardOriginalServices = CloneServiceItems(JobCardEditor.Services);
        JobCardOriginalMinutes = GetJobCardEstimatedMinutes(JobCardEditor);
        JobCardOriginalTotalPrice = GetJobCardServicesTotal(JobCardEditor);

        if (showSuccessMessage)
            JobCardMessage = "Job card saved.";
    }

    private async Task SendCustomerSms()
    {
        if (!JobCardBookingId.HasValue || JobCardEditor is null)
            return;

        var booking = Data.Bookings.FirstOrDefault(b => b.Id == JobCardBookingId.Value && b.StoreId == StoreId);
        if (booking is null)
            return;

        var recipient = (booking.CustomerPhone ?? "").Trim();
        if (string.IsNullOrWhiteSpace(recipient))
        {
            JobCardMessage = "No customer phone number is available for this booking.";
            return;
        }

        var message = string.IsNullOrWhiteSpace(JobCardEditor.CommunicationDraft)
            ? $"Update on your booking #{booking.Id}: your bike is being worked on."
            : JobCardEditor.CommunicationDraft.Trim();

        JobCardEditor.LastSmsSentUtc = DateTime.UtcNow;
        JobCardEditor.MessageLog.Add(new JobCardMessageLogItem
        {
            SentAtUtc = DateTime.UtcNow,
            Channel = "SMS (simulation)",
            Recipient = recipient,
            Summary = SummarizeMessage(message)
        });

        await PersistJobCardAsync(showSuccessMessage: false, skipOverrunWarnings: true);
        JobCardMessage = "SMS logged. Live SMS delivery requires an SMS/EPOS provider integration.";
    }

    private async Task SendCustomerEmail()
    {
        if (!JobCardBookingId.HasValue || JobCardEditor is null)
            return;

        var booking = Data.Bookings.FirstOrDefault(b => b.Id == JobCardBookingId.Value && b.StoreId == StoreId);
        if (booking is null)
            return;

        var recipient = (booking.CustomerEmail ?? "").Trim();
        if (string.IsNullOrWhiteSpace(recipient))
        {
            JobCardMessage = "No customer email address is available for this booking.";
            return;
        }

        var message = string.IsNullOrWhiteSpace(JobCardEditor.CommunicationDraft)
            ? $"Update on your booking #{booking.Id}: your bike is being worked on."
            : JobCardEditor.CommunicationDraft.Trim();

        try
        {
            var htmlBody = $"<p>{System.Net.WebUtility.HtmlEncode(message).Replace("\n", "<br />")}</p>";
            await EmailSender.SendAsync(
                recipient,
                $"Workshop update for booking #{booking.Id}",
                htmlBody,
                message);

            JobCardEditor.LastEmailSentUtc = DateTime.UtcNow;
            JobCardEditor.MessageLog.Add(new JobCardMessageLogItem
            {
                SentAtUtc = DateTime.UtcNow,
                Channel = "Email",
                Recipient = recipient,
                Summary = SummarizeMessage(message)
            });

            await PersistJobCardAsync(showSuccessMessage: false, skipOverrunWarnings: true);
            JobCardMessage = $"Email sent to {recipient}.";
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to send job card email for booking {BookingId}", booking.Id);
            JobCardMessage = "Email send failed. Check Postmark configuration and try again.";
        }
    }

}
