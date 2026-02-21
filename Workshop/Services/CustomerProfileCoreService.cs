using Workshop.Models;

namespace Workshop.Services;

public static class CustomerProfileCoreService
{
    public static CustomerProfile? FindCustomerProfileCore(
        IEnumerable<CustomerProfile> profiles,
        string? accountNumber,
        string? email,
        string? phone)
    {
        var account = (accountNumber ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(account))
        {
            var byAccount = profiles.FirstOrDefault(c =>
                c.AccountNumber.Equals(account, StringComparison.OrdinalIgnoreCase));
            if (byAccount is not null)
                return byAccount;
        }

        var normalizedEmail = (email ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(normalizedEmail))
        {
            var byEmail = profiles.FirstOrDefault(c =>
                c.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase));
            if (byEmail is not null)
                return byEmail;
        }

        var normalizedPhone = NormalizeDigits(phone ?? "");
        if (!string.IsNullOrWhiteSpace(normalizedPhone))
        {
            var byPhone = profiles.FirstOrDefault(c =>
                (!string.IsNullOrWhiteSpace(c.PhoneNormalized)
                    ? c.PhoneNormalized.Equals(normalizedPhone, StringComparison.Ordinal)
                    : NormalizeDigits(c.Phone).Equals(normalizedPhone, StringComparison.Ordinal)));
            if (byPhone is not null)
                return byPhone;
        }

        return null;
    }

    public static string GenerateNextCustomerAccountNumber(IEnumerable<CustomerProfile> profiles)
    {
        var highest = 0;
        foreach (var account in profiles.Select(c => c.AccountNumber))
        {
            if (string.IsNullOrWhiteSpace(account))
                continue;

            var digits = new string(account.Where(char.IsDigit).ToArray());
            if (int.TryParse(digits, out var parsed) && parsed > highest)
                highest = parsed;
        }

        return $"CUST-{highest + 1:00000}";
    }

    public static CustomerProfile CloneCustomerProfile(CustomerProfile source)
    {
        return new CustomerProfile
        {
            Id = source.Id,
            TenantId = source.TenantId,
            AccountNumber = source.AccountNumber,
            FirstName = source.FirstName,
            LastName = source.LastName,
            Phone = source.Phone,
            PhoneNormalized = source.PhoneNormalized,
            Email = source.Email,
            County = source.County,
            Postcode = source.Postcode,
            AddressLine1 = source.AddressLine1,
            AddressLine2 = source.AddressLine2,
            CreatedUtc = source.CreatedUtc,
            UpdatedUtc = source.UpdatedUtc,
            Bikes = source.Bikes.Select(CloneBike).ToList(),
            Quotes = source.Quotes.Select(CloneQuote).ToList(),
            Communications = source.Communications.Select(CloneCommunication).ToList()
        };
    }

    public static string NormalizeDigits(string input) =>
        new(input.Where(char.IsDigit).ToArray());

    private static CustomerBikeProfile CloneBike(CustomerBikeProfile bike)
    {
        return new CustomerBikeProfile
        {
            Id = bike.Id,
            Make = bike.Make,
            Model = bike.Model,
            Size = bike.Size,
            FrameNumber = bike.FrameNumber,
            StockNumber = bike.StockNumber
        };
    }

    private static CustomerQuoteRecord CloneQuote(CustomerQuoteRecord quote)
    {
        return new CustomerQuoteRecord
        {
            Id = quote.Id,
            CreatedUtc = quote.CreatedUtc,
            Status = quote.Status,
            StatusUpdatedUtc = quote.StatusUpdatedUtc,
            SentAtUtc = quote.SentAtUtc,
            AcceptedAtUtc = quote.AcceptedAtUtc,
            ExpiresAtUtc = quote.ExpiresAtUtc,
            StatusDetail = quote.StatusDetail,
            StoreId = quote.StoreId,
            StoreName = quote.StoreName,
            BikeDetails = quote.BikeDetails,
            JobIds = (quote.JobIds ?? Array.Empty<string>()).ToArray(),
            JobNames = (quote.JobNames ?? Array.Empty<string>()).ToArray(),
            EstimatedMinutes = quote.EstimatedMinutes,
            EstimatedPriceIncVat = quote.EstimatedPriceIncVat,
            Notes = quote.Notes,
            CreatedBy = quote.CreatedBy
        };
    }

    private static CustomerCommunicationRecord CloneCommunication(CustomerCommunicationRecord communication)
    {
        return new CustomerCommunicationRecord
        {
            Id = communication.Id,
            SentAtUtc = communication.SentAtUtc,
            Channel = communication.Channel,
            Direction = communication.Direction,
            Recipient = communication.Recipient,
            Summary = communication.Summary,
            DeliveryStatus = communication.DeliveryStatus,
            DeliveryError = communication.DeliveryError,
            IsAutomated = communication.IsAutomated,
            Source = communication.Source
        };
    }
}
