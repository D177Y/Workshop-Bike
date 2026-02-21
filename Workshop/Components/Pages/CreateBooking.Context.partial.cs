using Workshop.Models;

namespace Workshop.Components.Pages;

public partial class CreateBooking
{
    private IEnumerable<Mechanic> MechanicsForSelectedStore =>
        Data.Mechanics.Where(m =>
            m.StoreId == SelectedStoreId &&
            _userAccess.CanAccessMechanic(m) &&
            CanMechanicDoJobs(m, SelectedJobsForPricing));

    private IEnumerable<Store> StoresForCurrentUser =>
        Data.Stores
            .Where(s => _userAccess.CanAccessStore(s.Id))
            .OrderBy(s => s.Name);

    private bool HasStoreAccess => StoresForCurrentUser.Any();

    private CustomerBikeRow? SelectedBike =>
        string.IsNullOrWhiteSpace(_selectedBikeId)
            ? null
            : CustomerBikes.FirstOrDefault(b => b.RowId.Equals(_selectedBikeId, StringComparison.OrdinalIgnoreCase));

    private Store? GetSelectedStore()
    {
        if (SelectedStoreId == 0)
            return null;

        return StoresForCurrentUser.FirstOrDefault(s => s.Id == SelectedStoreId);
    }

    private string SummaryStore => StoresForCurrentUser.FirstOrDefault(s => s.Id == SelectedStoreId)?.Name ?? "-";
    private string SummaryMechanic => SelectedMechanicId == 0 ? "All" : Data.Mechanics.FirstOrDefault(m => m.Id == SelectedMechanicId)?.Name ?? "-";
    private string SummaryJobs => SelectedJobsForPricing.Any() ? string.Join(", ", SelectedJobsForPricing.Select(j => j.Name)) : "-";
    private string SummaryNotes => string.IsNullOrWhiteSpace(JobNotes) ? "-" : JobNotes;
    private string SummarySelectedDate => EarliestDay.HasValue ? EarliestDay.Value.ToString("dd MMM yyyy") : "-";
    private string SummaryEarliestDate => EarliestAvailableDay.HasValue ? EarliestAvailableDay.Value.ToString("dd MMM yyyy") : "-";
    private string SummaryCustomer => string.IsNullOrWhiteSpace(CustomerFirstName) && string.IsNullOrWhiteSpace(CustomerLastName)
        ? "-"
        : string.Join(" ", new[] { CustomerFirstName, CustomerLastName }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
    private string SummaryPhone => string.IsNullOrWhiteSpace(CustomerPhone) ? "-" : CustomerPhone;
    private string SummaryBike => string.IsNullOrWhiteSpace(BikeDetails) ? "-" : BikeDetails;
}
