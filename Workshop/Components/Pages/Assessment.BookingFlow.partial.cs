using Workshop.Models;
using Workshop.Services;

namespace Workshop.Components.Pages;

public partial class Assessment
{
    private async Task SendToBooking()
    {
        if (!HasStoreAccess || SelectedStoreId == 0)
        {
            _statusMessage = "No accessible store is available for this assessment.";
            _statusIsError = true;
            return;
        }

        var (customer, quote) = await SaveCurrentQuoteAsync(showSuccessMessage: false);
        if (customer is null || quote is null)
            return;

        var bikeDetails = string.IsNullOrWhiteSpace(quote.BikeDetails) ? SelectedBikeDetails : quote.BikeDetails;
        var bookingNotes = quote.Notes;
        var queryParts = new List<string>();

        AddQuery(queryParts, "storeId", (quote.StoreId > 0 ? quote.StoreId : SelectedStoreId).ToString());
        AddQuery(queryParts, "quoteId", quote.Id);
        AddQuery(queryParts, "jobs", quote.JobIds.Length == 0 ? string.Join(",", SelectedQuoteJobs.Select(j => j.Id)) : string.Join(",", quote.JobIds));
        AddQuery(queryParts, "manual", BuildManualQuoteOverridesQueryValue());
        AddQuery(queryParts, "notes", bookingNotes);
        AddQuery(queryParts, "customerAccountNumber", customer.AccountNumber);
        AddQuery(queryParts, "customerFirstName", customer.FirstName);
        AddQuery(queryParts, "customerLastName", customer.LastName);
        AddQuery(queryParts, "customerPhone", customer.Phone);
        AddQuery(queryParts, "customerEmail", customer.Email);
        AddQuery(queryParts, "customerCounty", customer.County);
        AddQuery(queryParts, "customerPostcode", customer.Postcode);
        AddQuery(queryParts, "customerAddressLine1", customer.AddressLine1);
        AddQuery(queryParts, "customerAddressLine2", customer.AddressLine2);
        AddQuery(queryParts, "bikeDetails", bikeDetails);

        var target = "/create-booking";
        if (queryParts.Count > 0)
            target += "?" + string.Join("&", queryParts);

        Nav.NavigateTo(target);
    }

    private string BuildBookingNotes() =>
        AssessmentNotesService.BuildBookingNotes(ActiveChecks.Select(c => c.Note));

    private static void AddQuery(List<string> queryParts, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        queryParts.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");
    }
}
