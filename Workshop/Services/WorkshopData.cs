using Workshop.Models;

namespace Workshop.Services;

public sealed class WorkshopData
{
    public List<Store> Stores { get; } = new()
    {
        new Store
        {
            Id = 1,
            Name = "Taunton",
            OpenFrom = new TimeSpan(9, 0, 0),
            OpenTo = new TimeSpan(17, 30, 0),
            DaysOpen = new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday }
        },
        new Store
        {
            Id = 2,
            Name = "Yeovil",
            OpenFrom = new TimeSpan(9, 0, 0),
            OpenTo = new TimeSpan(17, 30, 0),
            DaysOpen = new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday }
        },
        new Store
        {
            Id = 3,
            Name = "Bristol",
            OpenFrom = new TimeSpan(9, 0, 0),
            OpenTo = new TimeSpan(17, 30, 0),
            DaysOpen = new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday }
        },
        new Store
        {
            Id = 4,
            Name = "Bridgwater",
            OpenFrom = new TimeSpan(9, 0, 0),
            OpenTo = new TimeSpan(17, 30, 0),
            DaysOpen = new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday }
        },
        new Store
        {
            Id = 5,
            Name = "Weston",
            OpenFrom = new TimeSpan(9, 0, 0),
            OpenTo = new TimeSpan(17, 30, 0),
            DaysOpen = new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday }
        },
        new Store
        {
            Id = 6,
            Name = "Hereford",
            OpenFrom = new TimeSpan(9, 0, 0),
            OpenTo = new TimeSpan(17, 30, 0),
            DaysOpen = new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday }
        },
    };

    public List<Mechanic> Mechanics { get; } = new()
    {
        // Taunton
        new Mechanic { Id = 101, StoreId = 1, Name = "Dan", MaxBookableHoursPerDay = 6.0 },
        new Mechanic { Id = 102, StoreId = 1, Name = "Lee", MaxBookableHoursPerDay = 5.5 },
        new Mechanic { Id = 103, StoreId = 1, Name = "Sam", MaxBookableHoursPerDay = 6.0 },

        // Yeovil
        new Mechanic { Id = 201, StoreId = 2, Name = "Mitch", MaxBookableHoursPerDay = 6.0 },
        new Mechanic { Id = 202, StoreId = 2, Name = "Alex", MaxBookableHoursPerDay = 6.0 },

        // Bristol
        new Mechanic { Id = 301, StoreId = 3, Name = "Chris", MaxBookableHoursPerDay = 6.0 },
        new Mechanic { Id = 302, StoreId = 3, Name = "Jamie", MaxBookableHoursPerDay = 6.0 },
    };

    // Bookings are what the scheduler displays
    public List<Booking> Bookings { get; } = new()
    {
        new Booking
        {
            Id = 1,
            StoreId = 1,
            MechanicId = 101,
            Title = "Bronze service",
            Start = DateTime.Today.AddHours(9),
            End = DateTime.Today.AddHours(10),
            JobId = "SVC_BRONZE",
            AddOnIds = Array.Empty<string>(),
            TotalMinutes = 60,
            TotalPriceIncVat = 79.00m
        },
        new Booking
        {
            Id = 2,
            StoreId = 1,
            MechanicId = 102,
            Title = "Gold service",
            Start = DateTime.Today.AddHours(11),
            End = DateTime.Today.AddHours(12).AddMinutes(30),
            JobId = "SVC_GOLD",
            AddOnIds = Array.Empty<string>(),
            TotalMinutes = 90,
            TotalPriceIncVat = 149.00m
        }
    };

    private int _nextBookingId = 1000;

    public int NextBookingId() => Interlocked.Increment(ref _nextBookingId);
}
