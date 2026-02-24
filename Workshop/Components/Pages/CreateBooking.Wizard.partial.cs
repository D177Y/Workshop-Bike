using Syncfusion.Blazor.Navigations;
using Workshop.Services;

namespace Workshop.Components.Pages;

public partial class CreateBooking
{
    private void SetSelectedStoreId(int value) => SelectedStoreId = value;
    private void SetSelectedMechanicId(int value) => SelectedMechanicId = value;
    private void SetJobFilter(string value) => JobFilter = value ?? "";
    private void SetPackageServiceFilter(string value) => PackageServiceFilter = value ?? "";
    private void SetJobNotes(string value) => JobNotes = value ?? "";

    private void OnServiceDetailsJobToggle(CreateBookingServiceDetailsStep.JobToggleRequest request)
        => ToggleJob(request.Job, request.Args);

    private void OnServiceDetailsPackageToggle(CreateBookingServiceDetailsStep.PackageServiceToggleRequest request)
        => TogglePackageService(request.ServiceId, request.Args);

    private void OnManualServiceMinutesChanged(CreateBookingServiceDetailsStep.ManualServiceMinutesUpdateRequest request)
        => SetManualServiceMinutes(request.JobId, request.Args);

    private void OnManualServicePriceChanged(CreateBookingServiceDetailsStep.ManualServicePriceUpdateRequest request)
        => SetManualServicePrice(request.JobId, request.Args);

    private void NextStep()
    {
        if (Wizard.ActiveStep == 0 && !ValidateManualServicePricingInputs(out var validationMessage))
        {
            Message = validationMessage;
            ShowValidation = true;
            return;
        }

        Wizard.MoveNext(CanProceed, lastStep: 3);
    }

    private void PrevStep()
    {
        Wizard.MoveBack();
    }

    private void OnStepClicked(StepperClickedEventArgs args)
    {
        Wizard.SetActiveFromStepper(args.ActiveStep);
    }

    private bool CanProceed => CreateBookingProgressValidator.CanProceed(new CreateBookingProgressState(
        ActiveStep: Wizard.ActiveStep,
        HasStore: SelectedStoreId != 0,
        HasJobs: SelectedJobIds.Count > 0,
        HasSelectedDate: EarliestDay.HasValue,
        HasCustomerFirstName: !string.IsNullOrWhiteSpace(CustomerFirstName),
        HasCustomerLastName: !string.IsNullOrWhiteSpace(CustomerLastName),
        HasCustomerPhone: !string.IsNullOrWhiteSpace(CustomerPhone),
        HasCustomerEmail: !string.IsNullOrWhiteSpace(CustomerEmail),
        HasBikeDetails: !string.IsNullOrWhiteSpace(BikeDetails)));

}
