using Workshop.Models;

namespace Workshop.Services;

public static class WorkshopCacheMutationService
{
    public static List<Store> AddStore(List<Store> stores, Store store)
    {
        stores.Add(store);
        return stores.OrderBy(s => s.Name).ToList();
    }

    public static List<Store> RemoveStore(List<Store> stores, int storeId)
    {
        stores.RemoveAll(s => s.Id == storeId);
        return stores;
    }

    public static List<Mechanic> AddMechanic(List<Mechanic> mechanics, Mechanic mechanic)
    {
        mechanics.Add(mechanic);
        return mechanics.OrderBy(m => m.Name).ToList();
    }

    public static List<Mechanic> RemoveMechanic(List<Mechanic> mechanics, int mechanicId)
    {
        mechanics.RemoveAll(m => m.Id == mechanicId);
        return mechanics;
    }

    public static void RemoveBookingsByStore(List<Booking> bookings, int storeId) =>
        bookings.RemoveAll(b => b.StoreId == storeId);

    public static void RemoveBookingsByMechanic(List<Booking> bookings, int mechanicId) =>
        bookings.RemoveAll(b => b.MechanicId == mechanicId);

    public static void RemoveBookingsById(List<Booking> bookings, int bookingId) =>
        bookings.RemoveAll(b => b.Id == bookingId);

    public static void UpsertBooking(List<Booking> bookings, Booking booking)
    {
        var index = bookings.FindIndex(b => b.Id == booking.Id);
        if (index >= 0)
            bookings[index] = booking;
        else
            bookings.Add(booking);
    }

    public static void RemoveTimeOffByStore(List<MechanicTimeOff> entries, int storeId) =>
        entries.RemoveAll(t => t.StoreId == storeId);

    public static void RemoveTimeOffByMechanic(List<MechanicTimeOff> entries, int mechanicId) =>
        entries.RemoveAll(t => t.MechanicId == mechanicId);

    public static void RemoveTimeOffById(List<MechanicTimeOff> entries, int id) =>
        entries.RemoveAll(t => t.Id == id);

    public static List<MechanicTimeOff> UpsertAndOrderTimeOff(
        List<MechanicTimeOff> entries,
        MechanicTimeOff entry)
    {
        var index = entries.FindIndex(t => t.Id == entry.Id);
        if (index >= 0)
            entries[index] = entry;
        else
            entries.Add(entry);

        return TimeOffOrderingService.OrderBySoonest(entries);
    }
}
