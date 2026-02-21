namespace Workshop.Services;

public sealed class CreateBookingStepState
{
    public int ActiveStep { get; private set; }
    public bool ShowValidation { get; private set; }

    public void SetValidation(bool value) => ShowValidation = value;
    public void SetActiveStep(int value) => ActiveStep = Math.Max(0, value);

    public void MoveNext(bool canProceed, int lastStep)
    {
        if (!canProceed)
        {
            ShowValidation = true;
            return;
        }

        ShowValidation = false;
        if (ActiveStep < lastStep)
            ActiveStep++;
    }

    public void MoveBack()
    {
        ShowValidation = false;
        if (ActiveStep > 0)
            ActiveStep--;
    }

    public void SetActiveFromStepper(int targetStep)
    {
        if (targetStep > ActiveStep)
            return;

        ShowValidation = false;
        ActiveStep = targetStep;
    }
}
