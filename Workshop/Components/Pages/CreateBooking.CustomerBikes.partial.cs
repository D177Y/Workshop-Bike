using Workshop.Models;
using Workshop.Services;

namespace Workshop.Components.Pages;

public partial class CreateBooking
{
    private void SetCustomerAccountNumber(string value) => CustomerAccountNumber = value;
    private void SetCustomerFirstName(string value) => CustomerFirstName = value;
    private void SetCustomerLastName(string value) => CustomerLastName = value;
    private void SetCustomerPhone(string value) => CustomerPhone = value;
    private void SetCustomerEmail(string value) => CustomerEmail = value;

    private void ApplyCustomerFromProfile(CustomerProfile profile)
    {
        CustomerAccountNumber = profile.AccountNumber;
        if (string.IsNullOrWhiteSpace(CustomerFirstName) || profile.AccountNumber.Equals(CustomerAccountNumber, StringComparison.OrdinalIgnoreCase))
            CustomerFirstName = profile.FirstName;
        if (string.IsNullOrWhiteSpace(CustomerLastName) || profile.AccountNumber.Equals(CustomerAccountNumber, StringComparison.OrdinalIgnoreCase))
            CustomerLastName = profile.LastName;
        if (string.IsNullOrWhiteSpace(CustomerPhone) || profile.AccountNumber.Equals(CustomerAccountNumber, StringComparison.OrdinalIgnoreCase))
            CustomerPhone = profile.Phone;
        if (string.IsNullOrWhiteSpace(CustomerEmail) || profile.AccountNumber.Equals(CustomerAccountNumber, StringComparison.OrdinalIgnoreCase))
            CustomerEmail = profile.Email;
    }

    private void LoadCustomerBikes(IEnumerable<CustomerBikeProfile> bikes)
    {
        CustomerBikes.Clear();
        foreach (var bike in bikes)
        {
            CustomerBikes.Add(new CustomerBikeRow
            {
                RowId = string.IsNullOrWhiteSpace(bike.Id) ? NewBikeRowId() : bike.Id,
                Make = bike.Make,
                Model = bike.Model,
                Size = bike.Size,
                FrameNumber = bike.FrameNumber,
                StockNumber = bike.StockNumber
            });
        }

        if (!string.IsNullOrWhiteSpace(BikeDetails) && TrySelectBikeByDetails(BikeDetails))
            return;

        _selectedBikeId = CustomerBikes.FirstOrDefault()?.RowId;
        BikeDetails = BuildBikeDetailsText(SelectedBike);
    }

    private bool TrySelectBikeByDetails(string details)
    {
        var trimmed = details.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return false;

        var match = CustomerBikes.FirstOrDefault(b =>
            BuildBikeDetailsText(b).Equals(trimmed, StringComparison.OrdinalIgnoreCase));
        if (match is null)
            return false;

        _selectedBikeId = match.RowId;
        BikeDetails = BuildBikeDetailsText(match);
        return true;
    }

    private Task OnSelectedBikeDetailsChanged(string bikeDetails)
    {
        BikeDetails = bikeDetails;
        return Task.CompletedTask;
    }

    private async Task OnBookingBikesChangedAsync()
    {
        BikeDetails = BuildBikeDetailsText(SelectedBike);
        await PersistCustomerBikeListAsync(forceCreateProfile: false);
    }

    private void ClearBikeSelection()
    {
        _selectedBikeId = null;
        BikeDetails = "";
    }

    private async Task<CustomerProfile?> FindCurrentCustomerProfileAsync()
    {
        var account = CustomerAccountNumber.Trim();
        if (!string.IsNullOrWhiteSpace(account))
        {
            var byAccount = await CustomerProfiles.FindByAccountAsync(account);
            if (byAccount is not null)
                return byAccount;
        }

        return await CustomerProfiles.FindAsync(account, CustomerEmail.Trim(), CustomerPhone.Trim());
    }

    private async Task<CustomerProfile?> PersistCustomerBikeListAsync(bool forceCreateProfile)
    {
        var existing = await FindCurrentCustomerProfileAsync();
        if (existing is null && !forceCreateProfile)
            return null;

        var hasCustomerNames = !string.IsNullOrWhiteSpace(CustomerFirstName) && !string.IsNullOrWhiteSpace(CustomerLastName);
        if (existing is null && !hasCustomerNames)
            return null;

        var profile = existing ?? new CustomerProfile();
        profile.AccountNumber = CustomerAccountNumber.Trim();
        profile.FirstName = CustomerFirstName.Trim();
        profile.LastName = CustomerLastName.Trim();
        profile.Phone = CustomerPhone.Trim();
        profile.Email = CustomerEmail.Trim();
        profile.Bikes = CustomerBikes
            .Select(b => new CustomerBikeProfile
            {
                Id = b.RowId,
                Make = b.Make,
                Model = b.Model,
                Size = b.Size,
                FrameNumber = b.FrameNumber,
                StockNumber = b.StockNumber
            })
            .ToList();

        var saved = await CustomerProfiles.UpsertAsync(profile);
        CustomerAccountNumber = saved.AccountNumber;

        if (existing is null && forceCreateProfile)
        {
            _customerBikeStatus = $"Created customer profile {saved.AccountNumber} and saved {saved.Bikes.Count} bike(s).";
            _customerBikeStatusIsError = false;
        }

        return saved;
    }

    private static string BuildBikeDetailsText(CustomerBikeRow? bike) =>
        BikeDetailsService.BuildBikeDetailsText(bike);

    private static CustomerBikeRow? ParseBikeFromDetails(string bikeDetails) =>
        BikeDetailsService.ParseBikeFromDetails(bikeDetails);

    private static string NewBikeRowId() => BikeDetailsService.NewBikeRowId();
}
