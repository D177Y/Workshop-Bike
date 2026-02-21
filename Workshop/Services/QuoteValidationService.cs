namespace Workshop.Services;

public static class QuoteValidationService
{
    public static QuoteValidationResult ValidateCanEmailQuote(string customerEmail)
    {
        var email = (customerEmail ?? "").Trim();
        if (string.IsNullOrWhiteSpace(email))
            return QuoteValidationResult.Invalid("Please add a customer email address before emailing the quote.");

        return QuoteValidationResult.Valid();
    }
}

public sealed record QuoteValidationResult(bool IsValid, string Message)
{
    public static QuoteValidationResult Valid() => new(true, "");
    public static QuoteValidationResult Invalid(string message) => new(false, message);
}
