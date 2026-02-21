using Syncfusion.Blazor.Schedule;
using Workshop.Components.Pages;
using Workshop.Models;

namespace Workshop.Services;

public sealed class StoreSchedulerAppointmentService
{
    private readonly WorkshopData _data;
    private readonly JobCatalogService _catalog;
    private readonly StoreSchedulerBookingProjectionService _projection;

    public StoreSchedulerAppointmentService(
        WorkshopData data,
        JobCatalogService catalog,
        StoreSchedulerBookingProjectionService projection)
    {
        _data = data;
        _catalog = catalog;
        _projection = projection;
    }

    public List<StoreScheduler.AppointmentVM> BuildAppointments(int storeId, IReadOnlyCollection<Mechanic> mechanics)
    {
        var mechanicIds = mechanics.Select(m => m.Id).ToHashSet();

        var bookingAppointments = _data.Bookings
            .Where(b => b.StoreId == storeId && mechanicIds.Contains(b.MechanicId))
            .Select(MapBookingToAppointment)
            .ToList();

        var timeOffAppointments = _data.MechanicTimeOffEntries
            .Where(t => t.StoreId == storeId && mechanicIds.Contains(t.MechanicId))
            .Select(MapTimeOffToAppointment)
            .ToList();

        return bookingAppointments
            .Concat(timeOffAppointments)
            .OrderBy(a => a.StartTime)
            .ThenBy(a => a.EndTime)
            .ToList();
    }

    public async Task PersistSchedulerChangeAsync(int storeId, StoreScheduler.AppointmentVM appt, ActionType actionType)
    {
        if (actionType == ActionType.EventCreate)
        {
            var title = appt.Subject?.Trim();
            if (string.IsNullOrWhiteSpace(title))
                title = "Manual booking";

            var booking = new Booking
            {
                StoreId = storeId,
                MechanicId = appt.MechanicId,
                Title = title,
                Start = appt.StartTime,
                End = appt.EndTime,
                JobId = "MANUAL",
                AddOnIds = Array.Empty<string>(),
                TotalMinutes = (int)(appt.EndTime - appt.StartTime).TotalMinutes,
                TotalPriceIncVat = 0
            };

            await _data.AddBookingAsync(booking);
            return;
        }

        if (actionType == ActionType.EventChange)
        {
            var booking = _data.Bookings.FirstOrDefault(x => x.StoreId == storeId && x.Id == appt.Id);
            if (booking is null)
                return;

            booking.MechanicId = appt.MechanicId;
            booking.Start = appt.StartTime;
            booking.End = appt.EndTime;
            booking.TotalMinutes = (int)(booking.End - booking.Start).TotalMinutes;

            await _data.UpdateBookingAsync(booking);
        }
    }

    private StoreScheduler.AppointmentVM MapBookingToAppointment(Booking booking)
    {
        return new StoreScheduler.AppointmentVM
        {
            Id = booking.Id,
            Subject = $"{booking.Title}  (£{booking.TotalPriceIncVat:0.00})",
            CategoryColor = BuildCssClasses(booking.JobId, booking.StatusName),
            StatusName = booking.StatusName,
            StartTime = booking.Start,
            EndTime = booking.End,
            MechanicId = booking.MechanicId,
            IsCustomPackage = _projection.IsCustomPackage(booking),
            ServiceLines = _projection.BuildServiceLines(booking),
            CustomerName = _projection.BuildCustomerName(booking),
            CustomerPhone = string.IsNullOrWhiteSpace(booking.CustomerPhone) ? "-" : booking.CustomerPhone.Trim(),
            BikeDetails = string.IsNullOrWhiteSpace(booking.BikeDetails) ? "-" : booking.BikeDetails.Trim(),
            Notes = _projection.BuildBookingNotes(booking)
        };
    }

    private static StoreScheduler.AppointmentVM MapTimeOffToAppointment(MechanicTimeOff entry)
    {
        var reason = string.IsNullOrWhiteSpace(entry.Type) ? "Time Off" : entry.Type.Trim();
        var notes = string.IsNullOrWhiteSpace(entry.Notes) ? "-" : entry.Notes.Trim();
        var syntheticId = -entry.Id;
        var reasonClass = $"timeoff-reason-{ReasonCssToken(reason)}";

        return new StoreScheduler.AppointmentVM
        {
            Id = syntheticId == 0 ? int.MinValue : syntheticId,
            Subject = $"Blocked: {reason}",
            CategoryColor = $"timeoff-block {reasonClass}",
            StatusName = reason,
            StartTime = entry.Start,
            EndTime = entry.End,
            MechanicId = entry.MechanicId,
            IsCustomPackage = false,
            ServiceLines = new List<string> { "Time off" },
            CustomerName = "-",
            CustomerPhone = "-",
            BikeDetails = "-",
            Notes = notes,
            IsTimeOff = true,
            TimeOffReason = reason
        };
    }

    private string BuildCssClasses(string jobId, string statusName)
    {
        var job = _catalog.Jobs.FirstOrDefault(j => j.Id == jobId);
        if (job is null)
            return "";

        var color = _catalog.ResolveJobColor(job);
        var statusColor = GetStatusColor(statusName);

        var jobClass = string.IsNullOrWhiteSpace(color) ? "" : $"job-color-{SanitizeColor(color)}";
        var statusClass = string.IsNullOrWhiteSpace(statusColor) ? "" : $"status-color-{SanitizeColor(statusColor)}";

        return $"{jobClass} {statusClass}".Trim();
    }

    private string GetStatusColor(string statusName)
    {
        var status = _data.Statuses.FirstOrDefault(s => s.Name.Equals(statusName, StringComparison.OrdinalIgnoreCase));
        return status?.ColorHex ?? "";
    }

    private static string SanitizeColor(string hex)
    {
        var value = hex.Trim().TrimStart('#');
        return value.ToLowerInvariant();
    }

    private static string ReasonCssToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "other";

        var token = value.Trim().ToLowerInvariant();
        token = token.Replace("&", "and", StringComparison.Ordinal);
        token = string.Join("-", token.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        return token switch
        {
            "shop-floor" => "shop-floor",
            "holiday" => "holiday",
            "illness" => "illness",
            "training" => "training",
            "other" => "other",
            _ => "other"
        };
    }
}
