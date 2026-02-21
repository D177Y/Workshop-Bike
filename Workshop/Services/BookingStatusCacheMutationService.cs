using Workshop.Models;

namespace Workshop.Services;

public static class BookingStatusCacheMutationService
{
    public static List<BookingStatus> Upsert(List<BookingStatus> statuses, BookingStatus status)
    {
        var existing = statuses.FirstOrDefault(s => s.Name == status.Name);
        if (existing is null)
        {
            statuses.Add(status);
            return statuses;
        }

        existing.ColorHex = status.ColorHex;
        return statuses;
    }

    public static List<BookingStatus> RemoveByName(List<BookingStatus> statuses, string name)
    {
        statuses.RemoveAll(s => s.Name == name);
        return statuses;
    }
}
