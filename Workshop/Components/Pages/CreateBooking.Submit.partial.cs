using Workshop.Models;

namespace Workshop.Components.Pages;

public partial class CreateBooking
{
    private async Task Create()
    {
        Message = "";
        if (!SetupStatus.CanCreateBooking || !HasStoreAccess)
        {
            Message = "Booking is not available for your account.";
            return;
        }

        if (!CanProceed)
        {
            ShowValidation = true;
            return;
        }

        ShowValidation = false;

        if (!EarliestDay.HasValue)
        {
            Message = "Pick an earliest day first.";
            return;
        }

        var store = GetSelectedStore();
        if (store is null)
        {
            Message = "Selected store is not available for your account.";
            return;
        }

        if (!store.TryGetHours(EarliestDay.Value, out var openFrom, out var openTo))
        {
            Message = "Store is closed on that day. Please select another day.";
            return;
        }

        var selected = SelectedJobsForPricing.ToList();
        if (selected.Count == 0)
        {
            Message = "Pick at least one job.";
            return;
        }

        if (!ValidateManualServicePricingInputs(out var manualValidationMessage))
        {
            ShowValidation = true;
            Message = manualValidationMessage;
            return;
        }

        var package = SelectedServicePackage;
        var title = package is not null
            ? package.Name
            : selected.Count == 1
                ? selected[0].Name
                : "Custom package";
        var mins = CalculatedMinutes;
        var price = CalculatedPrice;
        var bikeDetails = string.IsNullOrWhiteSpace(BikeDetails) ? BuildBikeDetailsText(SelectedBike) : BikeDetails.Trim();
        if (string.IsNullOrWhiteSpace(bikeDetails))
        {
            Message = "Select one bike before creating the booking.";
            return;
        }

        int? mechanicFilter = _selectedMechanicId == 0 ? null : _selectedMechanicId;

        if (mechanicFilter.HasValue)
        {
            var mech = Data.Mechanics.FirstOrDefault(m => m.Id == mechanicFilter.Value);
            if (mech is null || !_userAccess.CanAccessMechanic(mech))
            {
                Message = "Selected mechanic is not available for your account.";
                return;
            }
            if (!CanMechanicDoJobs(mech, selected))
            {
                Message = $"{mech.Name} can't perform the selected jobs. Pick another mechanic or job.";
                return;
            }
        }

        var dayStart = EarliestDay.Value.Date.Add(openFrom);
        var dayEnd = EarliestDay.Value.Date.Add(openTo);

        var slot = Scheduler.FindFirstSlot(SelectedStoreId, mins, dayStart, mechanicFilter, GetAllSelectedJobIds());

        if (!slot.Found || slot.Start.Date != dayStart.Date || slot.End > dayEnd)
        {
            Message = "No availability on that day for the selected job duration.";
            return;
        }

        var savedProfile = await PersistCustomerBikeListAsync(forceCreateProfile: true);
        if (savedProfile is not null)
            CustomerAccountNumber = savedProfile.AccountNumber;

        var booking = new Booking
        {
            StoreId = SelectedStoreId,
            MechanicId = slot.MechanicId,
            Title = title,
            Start = slot.Start,
            End = slot.End,
            JobId = PrimaryJobId,
            JobIds = GetAllSelectedJobIds().ToArray(),
            AddOnIds = Array.Empty<string>(),
            TotalMinutes = mins,
            TotalPriceIncVat = price,
            CustomerAccountNumber = CustomerAccountNumber.Trim(),
            CustomerFirstName = CustomerFirstName.Trim(),
            CustomerLastName = CustomerLastName.Trim(),
            CustomerPhone = CustomerPhone.Trim(),
            CustomerEmail = CustomerEmail.Trim(),
            BikeDetails = bikeDetails,
            Notes = JobNotes.Trim(),
            SourceQuoteId = SourceQuoteId.Trim()
        };

        booking.JobCard = BuildInitialBookingJobCard(booking);

        await Data.AddBookingAsync(booking);

        if (!string.IsNullOrWhiteSpace(SourceQuoteId) && !string.IsNullOrWhiteSpace(CustomerAccountNumber))
            await QuoteWorkflow.MarkQuoteAcceptedAsync(CustomerAccountNumber.Trim(), SourceQuoteId.Trim(), booking.Id);

        Nav.NavigateTo($"/store/{SelectedStoreId}");
    }

    private bool CanMechanicDoJobs(Mechanic mech, IReadOnlyCollection<JobDefinition> jobs)
    {
        foreach (var job in jobs)
        {
            if (!Catalog.CanMechanicPerformJob(mech, job))
                return false;
        }

        return true;
    }

    private IReadOnlyCollection<JobDefinition> SelectedJobs =>
        Catalog.Jobs.Where(j => SelectedJobIds.Contains(j.Id)).ToList();

    private IReadOnlyCollection<JobDefinition> SelectedJobsForPricing =>
        Catalog.Jobs.Where(j => GetAllSelectedJobIds().Contains(j.Id, StringComparer.OrdinalIgnoreCase)).ToList();

    private string PrimaryJobId =>
        SelectedJobIds.FirstOrDefault() ?? "";

    private List<string> GetAllSelectedJobIds()
    {
        var all = SelectedJobIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (CanSelectPackageServices)
        {
            var packageId = SelectedPackageJobId;
            foreach (var id in SelectedPackageServices)
            {
                if (string.IsNullOrWhiteSpace(id) || IsServicePackage(id))
                    continue;

                if (string.IsNullOrWhiteSpace(packageId)
                    || !Catalog.IsServiceAvailableAsAdditionalService(id, packageId))
                    continue;

                if (!all.Contains(id, StringComparer.OrdinalIgnoreCase))
                    all.Add(id);
            }
        }

        return all;
    }

    private BookingJobCard BuildInitialBookingJobCard(Booking booking)
    {
        var card = new BookingJobCard
        {
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedUtc = DateTime.UtcNow,
            AssignedMechanicId = booking.MechanicId,
            StatusName = "Scheduled",
            CustomerNotes = (booking.Notes ?? "").Trim(),
            CommunicationDraft = $"Update on your booking ({booking.Title})."
        };

        var selectedJobIds = booking.JobIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var servicePackageId = selectedJobIds.FirstOrDefault(IsServicePackage);

        foreach (var jobId in selectedJobIds)
        {
            var job = Catalog.Jobs.FirstOrDefault(j => j.Id.Equals(jobId, StringComparison.OrdinalIgnoreCase));
            if (job is null)
                continue;

            var packageContext = Catalog.IsServicePackage(job.Id) ? null : servicePackageId;
            int minutes;
            decimal price;
            if (RequiresManualPricingAtUse(job))
            {
                minutes = GetManualServiceMinutes(job.Id);
                price = GetManualServicePrice(job.Id);
            }
            else
            {
                var priced = Catalog.PriceAndTime(job.Id, packageContext);
                minutes = priced.minutes;
                price = priced.priceIncVat;
            }

            card.Services.Add(new JobCardServiceItem
            {
                JobId = job.Id,
                Name = job.Name,
                Description = (job.Description ?? "").Trim(),
                EstimatedMinutes = Math.Max(0, minutes),
                EstimatedPriceIncVat = Math.Max(0m, price)
            });
        }

        if (card.Services.Count == 0)
        {
            card.Services.Add(new JobCardServiceItem
            {
                Name = string.IsNullOrWhiteSpace(booking.Title) ? "Workshop service" : booking.Title.Trim(),
                EstimatedMinutes = Math.Max(0, booking.TotalMinutes),
                EstimatedPriceIncVat = Math.Max(0m, booking.TotalPriceIncVat)
            });
        }

        return card;
    }
}
