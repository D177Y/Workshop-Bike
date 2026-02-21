namespace Workshop.Services;

public static class CreateBookingProgressValidator
{
    public static bool CanProceed(CreateBookingProgressState state)
    {
        return state.ActiveStep switch
        {
            0 => state.HasStore && state.HasJobs,
            1 => state.HasSelectedDate,
            2 => state.HasCustomerFirstName
                 && state.HasCustomerLastName
                 && state.HasCustomerPhone
                 && state.HasCustomerEmail
                 && state.HasBikeDetails,
            3 => state.HasStore
                 && state.HasJobs
                 && state.HasSelectedDate
                 && state.HasCustomerFirstName
                 && state.HasCustomerLastName
                 && state.HasCustomerPhone
                 && state.HasCustomerEmail
                 && state.HasBikeDetails,
            _ => false
        };
    }
}

public readonly record struct CreateBookingProgressState(
    int ActiveStep,
    bool HasStore,
    bool HasJobs,
    bool HasSelectedDate,
    bool HasCustomerFirstName,
    bool HasCustomerLastName,
    bool HasCustomerPhone,
    bool HasCustomerEmail,
    bool HasBikeDetails);
