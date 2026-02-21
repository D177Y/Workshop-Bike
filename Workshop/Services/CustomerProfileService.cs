using Workshop.Models;

namespace Workshop.Services;

public sealed class CustomerProfileService
{
    private readonly WorkshopData _data;

    public CustomerProfileService(WorkshopData data)
    {
        _data = data;
    }

    public async Task<CustomerProfile?> FindAsync(string? accountNumber, string? email, string? phone)
    {
        await _data.EnsureInitializedAsync();
        return await _data.FindCustomerProfileAsync(accountNumber, email, phone);
    }

    public async Task<CustomerProfile?> FindByAccountAsync(string? accountNumber)
    {
        await _data.EnsureInitializedAsync();
        return await _data.FindCustomerProfileByAccountAsync(accountNumber ?? "");
    }

    public async Task<CustomerProfile> UpsertAsync(CustomerProfileInput input)
    {
        await _data.EnsureInitializedAsync();

        var existing = await _data.FindCustomerProfileAsync(input.AccountNumber, input.Email, input.Phone);
        var profile = existing ?? new CustomerProfile();

        if (!string.IsNullOrWhiteSpace(input.AccountNumber))
            profile.AccountNumber = input.AccountNumber.Trim();

        profile.FirstName = input.FirstName.Trim();
        profile.LastName = input.LastName.Trim();
        profile.Phone = input.Phone.Trim();
        profile.Email = input.Email.Trim();
        profile.County = input.County.Trim();
        profile.Postcode = input.Postcode.Trim();
        profile.AddressLine1 = input.AddressLine1.Trim();
        profile.AddressLine2 = input.AddressLine2.Trim();
        profile.Bikes = input.Bikes
            .Select(b => new CustomerBikeProfile
            {
                Id = string.IsNullOrWhiteSpace(b.Id) ? Guid.NewGuid().ToString("N") : b.Id.Trim(),
                Make = (b.Make ?? "").Trim(),
                Model = (b.Model ?? "").Trim(),
                Size = (b.Size ?? "").Trim(),
                FrameNumber = (b.FrameNumber ?? "").Trim(),
                StockNumber = (b.StockNumber ?? "").Trim()
            })
            .Where(b => !string.IsNullOrWhiteSpace(b.Make) || !string.IsNullOrWhiteSpace(b.Model))
            .ToList();

        return await _data.SaveCustomerProfileAsync(profile);
    }

    public async Task<CustomerProfile> UpsertAsync(CustomerProfile profile)
    {
        await _data.EnsureInitializedAsync();
        return await _data.SaveCustomerProfileAsync(profile);
    }

    public async Task<CustomerProfile?> AppendCommunicationAsync(string accountNumber, CustomerCommunicationRecord communication)
    {
        await _data.EnsureInitializedAsync();
        return await _data.AppendCommunicationAsync(accountNumber, communication);
    }

    public static CustomerValidationResult ValidateRequiredFields(CustomerProfileInput input, bool requireEmail)
        => CustomerValidationService.ValidateRequiredFields(input, requireEmail);

    public static List<CustomerProfile> FindDuplicates(IEnumerable<CustomerProfile> customers, CustomerProfileInput input)
    {
        var requestPhoneDigits = NormalizeDigits(input.Phone);
        var requestAccount = input.AccountNumber.Trim();
        var requestEmail = input.Email.Trim();
        var requestLast = input.LastName.Trim();
        var requestPostcode = input.Postcode.Trim();

        return customers
            .Where(customer =>
                (!string.IsNullOrWhiteSpace(requestAccount) &&
                 customer.AccountNumber.Equals(requestAccount, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrWhiteSpace(requestPhoneDigits) &&
                    NormalizeDigits(customer.Phone).Equals(requestPhoneDigits, StringComparison.Ordinal))
                || (!string.IsNullOrWhiteSpace(requestEmail) &&
                    customer.Email.Equals(requestEmail, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrWhiteSpace(requestLast) &&
                    !string.IsNullOrWhiteSpace(requestPostcode) &&
                    customer.LastName.Equals(requestLast, StringComparison.OrdinalIgnoreCase) &&
                    customer.Postcode.Equals(requestPostcode, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    private static string NormalizeDigits(string input) =>
        new((input ?? "").Where(char.IsDigit).ToArray());
}
