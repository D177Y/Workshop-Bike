using Microsoft.AspNetCore.WebUtilities;

namespace Workshop.Components.Pages;

public partial class CreateBooking
{
    protected override async Task OnInitializedAsync()
    {
        await Data.EnsureInitializedAsync();
        await Catalog.EnsureInitializedAsync();
        SetupStatus = await SetupService.GetStatusAsync();
        _userAccess = await UserAccess.GetCurrentAsync();

        if (!SetupStatus.CanCreateBooking || !HasStoreAccess)
            return;

        SelectedStoreId = StoresForCurrentUser.First().Id;
        ApplyPrefillFromQuery();
        await InitializeBikeSelectionAsync();

        Recalc();
        UpdateEarliestAvailableDay();
        RefreshMonthAvailability();
        EnsureSelectedDayIsValid();
    }

    private void ApplyPrefillFromQuery()
    {
        if (_queryPrefillApplied)
            return;

        _queryPrefillApplied = true;

        var uri = Nav.ToAbsoluteUri(Nav.Uri);
        var query = QueryHelpers.ParseQuery(uri.Query);
        if (query.Count == 0)
            return;

        if (query.TryGetValue("storeId", out var storeValue)
            && int.TryParse(storeValue.ToString(), out var parsedStoreId)
            && StoresForCurrentUser.Any(s => s.Id == parsedStoreId))
        {
            SelectedStoreId = parsedStoreId;
        }

        if (query.TryGetValue("jobs", out var jobsValue))
        {
            var parsedJobs = jobsValue.ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(id => Catalog.Jobs.Any(j => j.Id.Equals(id, StringComparison.OrdinalIgnoreCase)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (parsedJobs.Count > 0)
            {
                var packageId = parsedJobs.FirstOrDefault(IsServicePackage);
                if (!string.IsNullOrWhiteSpace(packageId))
                {
                    SelectedJobIds = new List<string> { packageId };
                    SelectedPackageServices.Clear();
                    foreach (var serviceId in parsedJobs.Where(id => !IsServicePackage(id)))
                    {
                        if (Catalog.IsServiceAvailableAsAdditionalService(serviceId, packageId))
                            SelectedPackageServices.Add(serviceId);
                    }
                }
                else
                {
                    SelectedJobIds = parsedJobs;
                    SelectedPackageServices.Clear();
                }
            }
        }

        if (query.TryGetValue("addOns", out var addOnsValue))
        {
            ApplyLegacyAdditionalServicesFromQuery(addOnsValue.ToString());
        }

        SyncManualServicePricingInputs();

        if (query.TryGetValue("manual", out var manualValue))
        {
            ApplyManualPricingFromQuery(manualValue.ToString());
        }

        SyncManualServicePricingInputs();

        if (query.TryGetValue("notes", out var notesValue))
        {
            var notes = notesValue.ToString();
            if (!string.IsNullOrWhiteSpace(notes))
                JobNotes = notes;
        }

        if (query.TryGetValue("customerFirstName", out var customerFirstName))
            CustomerFirstName = customerFirstName.ToString();

        if (query.TryGetValue("customerAccountNumber", out var customerAccountNumber))
            CustomerAccountNumber = customerAccountNumber.ToString();

        if (query.TryGetValue("customerLastName", out var customerLastName))
            CustomerLastName = customerLastName.ToString();

        if (query.TryGetValue("customerPhone", out var customerPhone))
            CustomerPhone = customerPhone.ToString();

        if (query.TryGetValue("customerEmail", out var customerEmail))
            CustomerEmail = customerEmail.ToString();

        if (query.TryGetValue("bikeDetails", out var bikeDetails))
            BikeDetails = bikeDetails.ToString();

        if (query.TryGetValue("quoteId", out var quoteId))
            SourceQuoteId = quoteId.ToString();
    }

    private async Task InitializeBikeSelectionAsync()
    {
        await LoadCustomerBikesInternalAsync(showNotFoundMessage: false);

        if (!string.IsNullOrWhiteSpace(BikeDetails))
        {
            if (!TrySelectBikeByDetails(BikeDetails))
            {
                var parsed = ParseBikeFromDetails(BikeDetails);
                if (parsed is not null)
                {
                    CustomerBikes.Add(parsed);
                    _selectedBikeId = parsed.RowId;
                    BikeDetails = BuildBikeDetailsText(parsed);
                }
            }
        }
        else if (CustomerBikes.Count > 0 && string.IsNullOrWhiteSpace(_selectedBikeId))
        {
            _selectedBikeId = CustomerBikes[0].RowId;
            BikeDetails = BuildBikeDetailsText(CustomerBikes[0]);
        }
    }

    private async Task LoadCustomerBikes()
    {
        await LoadCustomerBikesInternalAsync(showNotFoundMessage: true);
    }

    private async Task LoadCustomerBikesInternalAsync(bool showNotFoundMessage)
    {
        _customerBikeStatus = "";
        _customerBikeStatusIsError = false;

        var profile = await FindCurrentCustomerProfileAsync();
        if (profile is null)
        {
            if (showNotFoundMessage)
            {
                _customerBikeStatus = "No existing customer profile was found. You can add a new bike and it will be saved when booking is created.";
                _customerBikeStatusIsError = true;
            }
            return;
        }

        ApplyCustomerFromProfile(profile);
        LoadCustomerBikes(profile.Bikes);

        _customerBikeStatus = $"Loaded {CustomerBikes.Count} bike(s) from customer {profile.AccountNumber}.";
        _customerBikeStatusIsError = false;
    }
}
