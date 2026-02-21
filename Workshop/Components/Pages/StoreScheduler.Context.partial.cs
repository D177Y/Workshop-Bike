using Workshop.Models;

namespace Workshop.Components.Pages;

public partial class StoreScheduler
{
    protected override async Task OnParametersSetAsync()
    {
        await Data.EnsureInitializedAsync();
        await Catalog.EnsureInitializedAsync();
        _userAccess = await UserAccess.GetCurrentAsync();

        var store = Data.Stores.FirstOrDefault(s => s.Id == StoreId);
        if (store is null)
        {
            SetStoreUnavailable("Store not found.");
            return;
        }

        if (!_userAccess.CanAccessStore(StoreId))
        {
            SetStoreUnavailable("You do not have access to this store schedule.");
            return;
        }

        StoreMissing = false;
        StoreMissingMessage = "";
        StoreName = store.Name;

        UpdateHoursForSelectedDate();

        Mechanics = Data.Mechanics
            .Where(m => m.StoreId == StoreId && _userAccess.CanAccessMechanic(m))
            .ToList();
        VisibleMechanicIds = Mechanics.Select(m => m.Id).ToHashSet();
        ReloadAppointmentsFromBookings();
    }

    private void SetStoreUnavailable(string message)
    {
        StoreMissing = true;
        StoreMissingMessage = message;
        StoreName = "";
        Mechanics = new List<Mechanic>();
        VisibleMechanicIds.Clear();
        Appointments = new List<AppointmentVM>();
        FilteredMechanics = new List<Mechanic>();
        FilteredAppointments = new List<AppointmentVM>();
    }
}
