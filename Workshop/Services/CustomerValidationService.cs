namespace Workshop.Services;

public static class CustomerValidationService
{
    public static CustomerValidationResult ValidateRequiredFields(CustomerProfileInput input, bool requireEmail)
    {
        if (string.IsNullOrWhiteSpace(input.FirstName) ||
            string.IsNullOrWhiteSpace(input.LastName) ||
            string.IsNullOrWhiteSpace(input.Phone))
        {
            return CustomerValidationResult.Invalid("Customer first name, last name and phone are required.");
        }

        if (requireEmail && string.IsNullOrWhiteSpace(input.Email))
            return CustomerValidationResult.Invalid("Please add a customer email address.");

        return CustomerValidationResult.Valid();
    }
}

public sealed record CustomerProfileInput(
    string AccountNumber,
    string FirstName,
    string LastName,
    string Phone,
    string Email,
    string County,
    string Postcode,
    string AddressLine1,
    string AddressLine2,
    IReadOnlyList<CustomerBikeInput> Bikes);

public sealed record CustomerBikeInput(
    string Id,
    string Make,
    string Model,
    string Size,
    string FrameNumber,
    string StockNumber);

public sealed record CustomerValidationResult(bool IsValid, string Message)
{
    public static CustomerValidationResult Valid() => new(true, "");
    public static CustomerValidationResult Invalid(string message) => new(false, message);
}
