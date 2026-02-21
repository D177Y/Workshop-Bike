using Microsoft.AspNetCore.Components;
using Workshop.Services;

namespace Workshop.Components.Pages;

public partial class StoreScheduler
{
    private void ApplyFilters()
    {
        FilteredMechanics = Mechanics.Where(m => VisibleMechanicIds.Contains(m.Id)).ToList();
        FilteredAppointments = Appointments.Where(a => VisibleMechanicIds.Contains(a.MechanicId)).ToList();
    }

    private async Task ToggleMechanic(int mechanicId, ChangeEventArgs e)
    {
        var isChecked = e?.Value is bool b && b;
        if (isChecked)
            VisibleMechanicIds.Add(mechanicId);
        else
            VisibleMechanicIds.Remove(mechanicId);

        ApplyFilters();
        if (ScheduleRef is not null)
            await ScheduleRef.RefreshEventsAsync();
    }

    private void UpdateHoursForSelectedDate()
    {
        var store = Data.Stores.FirstOrDefault(s => s.Id == StoreId);
        if (store is null)
            return;
        if (store.TryGetHours(SelectedDate, out var openFrom, out var openTo))
        {
            StartHourText = openFrom.ToString(@"hh\:mm");
            EndHourText = openTo.ToString(@"hh\:mm");
            return;
        }

        StartHourText = store.OpenFrom.ToString(@"hh\:mm");
        EndHourText = store.OpenTo.ToString(@"hh\:mm");
    }

    private string BuildCssClasses(string jobId, string statusName)
    {
        var job = Catalog.Jobs.FirstOrDefault(j => j.Id == jobId);
        if (job == null) return "";

        var color = Catalog.ResolveJobColor(job);
        var statusColor = GetStatusColor(statusName);

        var jobClass = string.IsNullOrWhiteSpace(color) ? "" : $"job-color-{StoreSchedulerColorService.SanitizeColor(color)}";
        var statusClass = string.IsNullOrWhiteSpace(statusColor) ? "" : $"status-color-{StoreSchedulerColorService.SanitizeColor(statusColor)}";

        return $"{jobClass} {statusClass}".Trim();
    }

    private IEnumerable<string> GetAllColors()
    {
        var colors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var color in Catalog.CategoryColors.Values)
        {
            if (!string.IsNullOrWhiteSpace(color))
                colors.Add(StoreSchedulerColorService.NormalizeColor(color));
        }

        foreach (var job in Catalog.Jobs)
        {
            var color = Catalog.ResolveJobColor(job);
            if (!string.IsNullOrWhiteSpace(color))
                colors.Add(StoreSchedulerColorService.NormalizeColor(color));
        }

        foreach (var status in Data.Statuses)
        {
            if (!string.IsNullOrWhiteSpace(status.ColorHex))
                colors.Add(StoreSchedulerColorService.NormalizeColor(status.ColorHex));
        }

        return colors;
    }

    private string GetStatusColor(string statusName)
    {
        var status = Data.Statuses.FirstOrDefault(s => s.Name.Equals(statusName, StringComparison.OrdinalIgnoreCase));
        return status?.ColorHex ?? "";
    }

    private async Task ChangeStatus(int bookingId, ChangeEventArgs e)
    {
        var value = e.Value?.ToString();
        if (string.IsNullOrWhiteSpace(value))
            return;

        var booking = Data.Bookings.FirstOrDefault(b => b.Id == bookingId);
        if (booking == null) return;

        booking.StatusName = value;
        if (booking.JobCard is not null)
            booking.JobCard.StatusName = value;

        await Data.UpdateBookingAsync(booking);
        ReloadAppointmentsFromBookings();
        _ = ScheduleRef?.RefreshEventsAsync();
    }
}
