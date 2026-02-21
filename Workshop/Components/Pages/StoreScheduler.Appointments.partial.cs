using Microsoft.AspNetCore.Components;
using Syncfusion.Blazor.Schedule;
using Workshop.Models;
using Workshop.Services;

namespace Workshop.Components.Pages;

public partial class StoreScheduler
{
    [Inject] private StoreSchedulerAppointmentService AppointmentService { get; set; } = default!;

    private void ReloadAppointmentsFromBookings()
    {
        Appointments = AppointmentService.BuildAppointments(StoreId, Mechanics);
        ApplyFilters();
    }

    private async Task OnActionBegin(ActionEventArgs<AppointmentVM> args)
    {
        if (BypassLimitOnce)
        {
            BypassLimitOnce = false;
            return;
        }

        if (args.ActionType != ActionType.EventCreate &&
            args.ActionType != ActionType.EventChange)
            return;

        AppointmentVM? appt = null;

        if (args.ActionType == ActionType.EventCreate)
            appt = args.AddedRecords?.FirstOrDefault();
        else if (args.ActionType == ActionType.EventChange)
            appt = args.ChangedRecords?.FirstOrDefault();

        if (appt is null)
            return;

        if (appt.IsTimeOff)
        {
            args.Cancel = true;
            return;
        }

        if (!Mechanics.Any(m => m.Id == appt.MechanicId))
        {
            args.Cancel = true;
            return;
        }

        var bookingIdToIgnore = args.ActionType == ActionType.EventChange ? appt.Id : (int?)null;
        var exceed = Scheduler.WouldExceedDailyLimit(StoreId, appt.MechanicId, appt.StartTime, appt.EndTime, bookingIdToIgnore);

        if (!exceed)
        {
            await PersistSchedulerChangeAsync(appt, args.ActionType);
            return;
        }

        args.Cancel = true;

        var mech = Mechanics.First(m => m.Id == appt.MechanicId);
        OverbookMessage =
            $"{mech.Name} has a daily cap of {mech.MaxBookableHoursPerDay:0.#}h.\n\n" +
            "This change would exceed that.\n\n" +
            "Allow once?";

        PendingAppointment = Clone(appt);
        PendingActionType = args.ActionType;
        IsOverbookDialogVisible = true;

        await InvokeAsync(StateHasChanged);
    }

    private async Task PersistSchedulerChangeAsync(AppointmentVM appt, ActionType actionType)
    {
        if (!Mechanics.Any(m => m.Id == appt.MechanicId))
            return;

        await AppointmentService.PersistSchedulerChangeAsync(StoreId, appt, actionType);
        ReloadAppointmentsFromBookings();
    }

    private Task OnOverbookCancel()
    {
        IsOverbookDialogVisible = false;
        PendingAppointment = null;
        PendingActionType = null;
        return InvokeAsync(StateHasChanged);
    }

    private async Task OnOverbookAllowOnce()
    {
        IsOverbookDialogVisible = false;

        if (PendingAppointment is null || PendingActionType is null || ScheduleRef is null)
            return;

        BypassLimitOnce = true;

        await PersistSchedulerChangeAsync(PendingAppointment, PendingActionType.Value);
        await ScheduleRef.RefreshEventsAsync();

        PendingAppointment = null;
        PendingActionType = null;

        await InvokeAsync(StateHasChanged);
    }

    private void CloseJobCardOverrunDialog()
    {
        IsJobCardOverrunDialogVisible = false;
        JobCardOverrunMessage = "";
    }

    private Task SaveJobCardAllowOverrun()
    {
        CloseJobCardOverrunDialog();
        return PersistJobCardAsync(showSuccessMessage: true, skipOverrunWarnings: true);
    }

    private static AppointmentVM Clone(AppointmentVM a) => new()
    {
        Id = a.Id,
        Subject = a.Subject,
        CategoryColor = a.CategoryColor,
        StatusName = a.StatusName,
        StartTime = a.StartTime,
        EndTime = a.EndTime,
        MechanicId = a.MechanicId,
        IsCustomPackage = a.IsCustomPackage,
        ServiceLines = a.ServiceLines.ToList(),
        CustomerName = a.CustomerName,
        CustomerPhone = a.CustomerPhone,
        BikeDetails = a.BikeDetails,
        Notes = a.Notes,
        IsTimeOff = a.IsTimeOff,
        TimeOffReason = a.TimeOffReason
    };

    private async Task OpenEdit(AppointmentVM appt)
    {
        if (ScheduleRef is null)
            return;

        await ScheduleRef.OpenEditorAsync(appt, CurrentAction.Save);
        await ScheduleRef.CloseQuickInfoPopupAsync();
    }

    private void OpenCancelDialog(int bookingId)
    {
        CancelBookingId = bookingId;
        IsCancelDialogVisible = true;
    }

    private void CloseCancelDialog()
    {
        IsCancelDialogVisible = false;
        CancelBookingId = null;
    }

    private async Task ConfirmCancelBooking()
    {
        if (!CancelBookingId.HasValue)
        {
            CloseCancelDialog();
            return;
        }

        await Data.DeleteBookingAsync(CancelBookingId.Value);

        ReloadAppointmentsFromBookings();
        CloseCancelDialog();
        if (ScheduleRef is not null)
            await ScheduleRef.CloseQuickInfoPopupAsync();
    }

    public sealed class AppointmentVM
    {
        public int Id { get; set; }
        public string Subject { get; set; } = "";
        public string CategoryColor { get; set; } = "";
        public string StatusName { get; set; } = "Scheduled";
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int MechanicId { get; set; }
        public bool IsCustomPackage { get; set; }
        public List<string> ServiceLines { get; set; } = new();
        public string CustomerName { get; set; } = "-";
        public string CustomerPhone { get; set; } = "-";
        public string BikeDetails { get; set; } = "-";
        public string Notes { get; set; } = "-";
        public bool IsTimeOff { get; set; }
        public string TimeOffReason { get; set; } = "";
    }
}
