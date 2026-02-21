using Workshop.Models;

namespace Workshop.Services;

public static class BookingInsertPolicy
{
    public static void NormalizeForInsert(Booking booking)
    {
        booking.Id = 0;
    }
}
