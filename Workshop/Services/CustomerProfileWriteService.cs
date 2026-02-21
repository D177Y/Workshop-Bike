using Microsoft.EntityFrameworkCore;
using Workshop.Data;
using Workshop.Models;

namespace Workshop.Services;

public sealed class CustomerProfileWriteService
{
    private readonly IDbContextFactory<WorkshopDbContext> _factory;

    public CustomerProfileWriteService(IDbContextFactory<WorkshopDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<CustomerProfile> SaveAsync(
        int tenantId,
        CustomerProfile profile,
        IReadOnlyCollection<CustomerProfile> existingProfiles)
    {
        var normalized = NormalizeCustomerProfile(CustomerProfileCoreService.CloneCustomerProfile(profile));
        normalized.TenantId = tenantId;
        if (string.IsNullOrWhiteSpace(normalized.AccountNumber))
            normalized.AccountNumber = CustomerProfileCoreService.GenerateNextCustomerAccountNumber(existingProfiles);

        normalized.UpdatedUtc = DateTime.UtcNow;
        if (normalized.CreatedUtc == default)
            normalized.CreatedUtc = normalized.UpdatedUtc;

        await using var db = await _factory.CreateDbContextAsync();
        CustomerProfile? existing = null;
        if (normalized.Id > 0)
        {
            existing = await db.CustomerProfiles.FirstOrDefaultAsync(c =>
                c.Id == normalized.Id && c.TenantId == normalized.TenantId);
        }

        existing ??= await db.CustomerProfiles.FirstOrDefaultAsync(c =>
            c.TenantId == normalized.TenantId &&
            c.AccountNumber == normalized.AccountNumber);

        if (existing is null)
        {
            existing = CustomerProfileCoreService.CloneCustomerProfile(normalized);
            db.CustomerProfiles.Add(existing);
        }
        else
        {
            normalized.Id = existing.Id;
            if (existing.CreatedUtc != default)
                normalized.CreatedUtc = existing.CreatedUtc;
            CopyCustomerProfile(normalized, existing);
            db.CustomerProfiles.Update(existing);
        }

        await db.SaveChangesAsync();
        return CustomerProfileCoreService.CloneCustomerProfile(existing);
    }

    public async Task<CustomerProfile?> AppendQuoteAsync(
        int tenantId,
        string accountNumber,
        CustomerQuoteRecord quote,
        IReadOnlyCollection<CustomerProfile> existingProfiles)
    {
        var existing = CustomerProfileCoreService.FindCustomerProfileCore(existingProfiles, accountNumber, null, null);
        if (existing is null)
            return null;

        var updated = CustomerProfileCoreService.CloneCustomerProfile(existing);
        updated.Quotes.Insert(0, NormalizeQuote(quote));
        if (updated.Quotes.Count > 200)
            updated.Quotes = updated.Quotes.Take(200).ToList();

        return await SaveAsync(tenantId, updated, existingProfiles);
    }

    public async Task<CustomerProfile?> UpdateQuoteStatusAsync(
        int tenantId,
        string accountNumber,
        string quoteId,
        string status,
        string? statusDetail,
        IReadOnlyCollection<CustomerProfile> existingProfiles)
    {
        var account = (accountNumber ?? "").Trim();
        var targetQuoteId = (quoteId ?? "").Trim();
        if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(targetQuoteId))
            return null;

        var existing = CustomerProfileCoreService.FindCustomerProfileCore(existingProfiles, account, null, null);
        if (existing is null)
            return null;

        var updated = CustomerProfileCoreService.CloneCustomerProfile(existing);
        var quote = updated.Quotes.FirstOrDefault(q =>
            q.Id.Equals(targetQuoteId, StringComparison.OrdinalIgnoreCase));
        if (quote is null)
            return null;

        QuoteLifecycleService.ApplyStatus(quote, status, DateTime.UtcNow, statusDetail);
        return await SaveAsync(tenantId, updated, existingProfiles);
    }

    public async Task<CustomerProfile?> AppendCommunicationAsync(
        int tenantId,
        string accountNumber,
        CustomerCommunicationRecord communication,
        IReadOnlyCollection<CustomerProfile> existingProfiles)
    {
        var existing = CustomerProfileCoreService.FindCustomerProfileCore(existingProfiles, accountNumber, null, null);
        if (existing is null)
            return null;

        var updated = CustomerProfileCoreService.CloneCustomerProfile(existing);
        updated.Communications.Insert(0, NormalizeCommunication(communication));
        if (updated.Communications.Count > 400)
            updated.Communications = updated.Communications.Take(400).ToList();

        return await SaveAsync(tenantId, updated, existingProfiles);
    }

    private static void CopyCustomerProfile(CustomerProfile source, CustomerProfile destination)
    {
        destination.AccountNumber = source.AccountNumber;
        destination.FirstName = source.FirstName;
        destination.LastName = source.LastName;
        destination.Phone = source.Phone;
        destination.PhoneNormalized = source.PhoneNormalized;
        destination.Email = source.Email;
        destination.County = source.County;
        destination.Postcode = source.Postcode;
        destination.AddressLine1 = source.AddressLine1;
        destination.AddressLine2 = source.AddressLine2;
        destination.CreatedUtc = source.CreatedUtc;
        destination.UpdatedUtc = source.UpdatedUtc;
        destination.Bikes = source.Bikes.Select(NormalizeBike).ToList();
        destination.Quotes = source.Quotes.Select(NormalizeQuote).ToList();
        destination.Communications = source.Communications.Select(NormalizeCommunication).ToList();
    }

    private static CustomerProfile NormalizeCustomerProfile(CustomerProfile profile)
    {
        profile.AccountNumber = (profile.AccountNumber ?? "").Trim();
        profile.FirstName = (profile.FirstName ?? "").Trim();
        profile.LastName = (profile.LastName ?? "").Trim();
        profile.Phone = (profile.Phone ?? "").Trim();
        profile.PhoneNormalized = NormalizeDigits(profile.Phone);
        profile.Email = (profile.Email ?? "").Trim();
        profile.County = (profile.County ?? "").Trim();
        profile.Postcode = (profile.Postcode ?? "").Trim();
        profile.AddressLine1 = (profile.AddressLine1 ?? "").Trim();
        profile.AddressLine2 = (profile.AddressLine2 ?? "").Trim();

        profile.Bikes = (profile.Bikes ?? new List<CustomerBikeProfile>())
            .Select(NormalizeBike)
            .Where(b => !string.IsNullOrWhiteSpace(b.Make) || !string.IsNullOrWhiteSpace(b.Model))
            .ToList();

        profile.Quotes = (profile.Quotes ?? new List<CustomerQuoteRecord>())
            .Select(NormalizeQuote)
            .OrderByDescending(q => q.CreatedUtc)
            .ToList();

        profile.Communications = (profile.Communications ?? new List<CustomerCommunicationRecord>())
            .Select(NormalizeCommunication)
            .OrderByDescending(c => c.SentAtUtc)
            .ToList();

        return profile;
    }

    private static CustomerBikeProfile NormalizeBike(CustomerBikeProfile bike)
    {
        return new CustomerBikeProfile
        {
            Id = string.IsNullOrWhiteSpace(bike.Id) ? Guid.NewGuid().ToString("N") : bike.Id.Trim(),
            Make = (bike.Make ?? "").Trim(),
            Model = (bike.Model ?? "").Trim(),
            Size = (bike.Size ?? "").Trim(),
            FrameNumber = (bike.FrameNumber ?? "").Trim(),
            StockNumber = (bike.StockNumber ?? "").Trim()
        };
    }

    private static CustomerQuoteRecord NormalizeQuote(CustomerQuoteRecord quote)
    {
        var createdUtc = quote.CreatedUtc == default ? DateTime.UtcNow : quote.CreatedUtc;
        var normalized = new CustomerQuoteRecord
        {
            Id = string.IsNullOrWhiteSpace(quote.Id) ? Guid.NewGuid().ToString("N") : quote.Id.Trim(),
            CreatedUtc = createdUtc,
            Status = QuoteLifecycleStatus.Normalize(quote.Status),
            StatusUpdatedUtc = quote.StatusUpdatedUtc == default ? createdUtc : quote.StatusUpdatedUtc,
            SentAtUtc = quote.SentAtUtc,
            AcceptedAtUtc = quote.AcceptedAtUtc,
            ExpiresAtUtc = quote.ExpiresAtUtc,
            StatusDetail = (quote.StatusDetail ?? "").Trim(),
            StoreId = quote.StoreId,
            StoreName = (quote.StoreName ?? "").Trim(),
            BikeDetails = (quote.BikeDetails ?? "").Trim(),
            JobIds = (quote.JobIds ?? Array.Empty<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            JobNames = (quote.JobNames ?? Array.Empty<string>())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim())
                .ToArray(),
            EstimatedMinutes = Math.Max(0, quote.EstimatedMinutes),
            EstimatedPriceIncVat = Math.Max(0, quote.EstimatedPriceIncVat),
            Notes = (quote.Notes ?? "").Trim(),
            CreatedBy = (quote.CreatedBy ?? "").Trim()
        };

        var resolvedStatus = QuoteLifecycleService.ResolveStatus(normalized, DateTime.UtcNow);
        var statusUpdatedUtc = resolvedStatus.Equals(normalized.Status, StringComparison.OrdinalIgnoreCase)
            ? normalized.StatusUpdatedUtc
            : DateTime.UtcNow;
        QuoteLifecycleService.ApplyStatus(normalized, resolvedStatus, statusUpdatedUtc, normalized.StatusDetail);
        if (!normalized.ExpiresAtUtc.HasValue)
            normalized.ExpiresAtUtc = normalized.CreatedUtc.AddDays(30);

        return normalized;
    }

    private static CustomerCommunicationRecord NormalizeCommunication(CustomerCommunicationRecord communication)
    {
        var sentAtUtc = communication.SentAtUtc == default ? DateTime.UtcNow : communication.SentAtUtc;
        return new CustomerCommunicationRecord
        {
            Id = string.IsNullOrWhiteSpace(communication.Id) ? Guid.NewGuid().ToString("N") : communication.Id.Trim(),
            SentAtUtc = sentAtUtc,
            Channel = (communication.Channel ?? "").Trim(),
            Direction = (communication.Direction ?? "").Trim(),
            Recipient = (communication.Recipient ?? "").Trim(),
            Summary = (communication.Summary ?? "").Trim(),
            DeliveryStatus = (communication.DeliveryStatus ?? "").Trim(),
            DeliveryError = (communication.DeliveryError ?? "").Trim(),
            IsAutomated = communication.IsAutomated,
            Source = (communication.Source ?? "").Trim()
        };
    }

    private static string NormalizeDigits(string input) =>
        CustomerProfileCoreService.NormalizeDigits(input);
}
