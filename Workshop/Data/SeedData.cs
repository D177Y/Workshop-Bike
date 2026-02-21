using Workshop.Models;

namespace Workshop.Data;

public static class SeedData
{
    public static Tenant DefaultTenant => new()
    {
        Id = 1,
        Name = "Demo Workshop",
        Code = "demo",
        CreatedAtUtc = DateTime.UtcNow
    };

    public static CatalogSettings DefaultCatalogSettings(int tenantId)
        => new()
        {
            TenantId = tenantId,
            AutomaticServicePricingEnabled = false,
            DefaultHourlyRate = 76m,
            DiscountedHourlyRate = 60m,
            LossLeaderHourlyRate = 50m,
            AutoPriceRoundingIncrement = 0.50m,
            AutoPriceRoundingMode = PriceRoundingMode.Down,
            ServicePackageAddOnTimeReductions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["SVC_BRONZE"] = 5,
                ["SVC_SILVER"] = 10,
                ["SVC_GOLD"] = 10
            },
            CategoryColors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Service Packages"] = "#4f46e5",
                ["Brake"] = "#ef4444",
                ["Gear"] = "#f59e0b",
                ["Wheel"] = "#10b981",
                ["Suspension"] = "#0ea5e9",
                ["Frame"] = "#8b5cf6",
                ["E-Bike"] = "#14b8a6",
                ["Warranty"] = "#64748b",
                ["Labour"] = "#f97316",
                ["Accessories"] = "#22c55e"
            },
            ServiceCategoryHierarchy = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Service Packages"] = new List<string>(),
                ["Brake"] = new List<string> { "Hydraulic", "Mechanical" },
                ["Gear"] = new List<string> { "Drivetrain" },
                ["Wheel"] = new List<string> { "Puncture / Tyre", "Wheel True", "Tubeless" },
                ["Suspension"] = new List<string> { "Service" },
                ["Frame"] = new List<string> { "Repair" },
                ["E-Bike"] = new List<string> { "Diagnostics" },
                ["Warranty"] = new List<string> { "Labour", "Send External" },
                ["Labour"] = new List<string> { "General" },
                ["Accessories"] = new List<string> { "Fitting Accessories" }
            },
            SkillLevels = new List<string>
            {
                "Beginner",
                "Intermediate",
                "Advanced",
                "Bike builder"
            }
        };

    public static List<JobDefinition> Jobs(int tenantId) => new()
    {
        new JobDefinition
        {
            Id = "SVC_BRONZE",
            TenantId = tenantId,
            Name = "Bronze service",
            DefaultMinutes = 60,
            BasePriceIncVat = 79.00m,
            Category = "Service Packages",
            SkillLevel = "Beginner",
            ColorHex = ""
        },
        new JobDefinition
        {
            Id = "SVC_SILVER",
            TenantId = tenantId,
            Name = "Silver service",
            DefaultMinutes = 75,
            BasePriceIncVat = 109.00m,
            Category = "Service Packages",
            SkillLevel = "Intermediate",
            ColorHex = ""
        },
        new JobDefinition
        {
            Id = "SVC_GOLD",
            TenantId = tenantId,
            Name = "Gold service",
            DefaultMinutes = 90,
            BasePriceIncVat = 149.00m,
            Category = "Service Packages",
            SkillLevel = "Advanced",
            ColorHex = ""
        },
        new JobDefinition
        {
            Id = "GEAR_CHAIN",
            TenantId = tenantId,
            Name = "Replace chain",
            DefaultMinutes = 20,
            BasePriceIncVat = 29.00m,
            Category = "Gear",
            SkillLevel = "Beginner",
            ColorHex = "",
            PackageOverrides = new List<JobServicePackageOverride>
            {
                new() { ServicePackageJobId = "SVC_BRONZE", Minutes = 25, PriceIncVat = 39.00m },
                new() { ServicePackageJobId = "SVC_SILVER", Minutes = 20, PriceIncVat = 35.00m },
                new() { ServicePackageJobId = "SVC_GOLD", Minutes = 20, PriceIncVat = 34.00m }
            }
        },
        new JobDefinition
        {
            Id = "BRAKE_BLEED",
            TenantId = tenantId,
            Name = "Brake bleed",
            DefaultMinutes = 30,
            BasePriceIncVat = 35.00m,
            Category = "Brake",
            SkillLevel = "Beginner",
            ColorHex = "",
            PackageOverrides = new List<JobServicePackageOverride>
            {
                new() { ServicePackageJobId = "SVC_BRONZE", Minutes = 60, PriceIncVat = 70.00m },
                new() { ServicePackageJobId = "SVC_SILVER", Minutes = 55, PriceIncVat = 70.00m },
                new() { ServicePackageJobId = "SVC_GOLD", Minutes = 50, PriceIncVat = 70.00m }
            }
        },
        new JobDefinition
        {
            Id = "EBIKE_SW",
            TenantId = tenantId,
            Name = "Software update",
            DefaultMinutes = 45,
            BasePriceIncVat = 59.00m,
            Category = "E-Bike",
            SkillLevel = "Intermediate",
            ColorHex = ""
        },
    };

    public static List<AddOnDefinition> AddOns(int tenantId) => new()
    {
        new AddOnDefinition
        {
            Id = "ADD_CHAIN",
            TenantId = tenantId,
            Name = "Replace chain",
            Category = "Accessories",
            Description = ""
        },
        new AddOnDefinition
        {
            Id = "ADD_BLEED",
            TenantId = tenantId,
            Name = "Brake bleed",
            Category = "Brake",
            Description = ""
        },
        new AddOnDefinition
        {
            Id = "ADD_TUBE",
            TenantId = tenantId,
            Name = "Replace inner tube",
            Category = "Wheel",
            Description = ""
        },
    };

    public static List<AddOnRule> AddOnRules(int tenantId) => new()
    {
        new AddOnRule { TenantId = tenantId, JobId = "SVC_BRONZE", AddOnId = "ADD_CHAIN", ExtraMinutes = 5, ExtraPriceIncVat = 10.00m },
        new AddOnRule { TenantId = tenantId, JobId = "SVC_SILVER", AddOnId = "ADD_CHAIN", ExtraMinutes = 0, ExtraPriceIncVat = 6.00m },
        new AddOnRule { TenantId = tenantId, JobId = "SVC_GOLD", AddOnId = "ADD_CHAIN", ExtraMinutes = 0, ExtraPriceIncVat = 5.00m },
        new AddOnRule { TenantId = tenantId, JobId = "SVC_BRONZE", AddOnId = "ADD_BLEED", ExtraMinutes = 30, ExtraPriceIncVat = 35.00m },
        new AddOnRule { TenantId = tenantId, JobId = "SVC_SILVER", AddOnId = "ADD_BLEED", ExtraMinutes = 25, ExtraPriceIncVat = 35.00m },
        new AddOnRule { TenantId = tenantId, JobId = "SVC_GOLD", AddOnId = "ADD_BLEED", ExtraMinutes = 20, ExtraPriceIncVat = 35.00m },
        new AddOnRule { TenantId = tenantId, JobId = "SVC_BRONZE", AddOnId = "ADD_TUBE", ExtraMinutes = 15, ExtraPriceIncVat = 15.00m },
        new AddOnRule { TenantId = tenantId, JobId = "SVC_SILVER", AddOnId = "ADD_TUBE", ExtraMinutes = 10, ExtraPriceIncVat = 15.00m },
        new AddOnRule { TenantId = tenantId, JobId = "SVC_GOLD", AddOnId = "ADD_TUBE", ExtraMinutes = 5, ExtraPriceIncVat = 15.00m },
    };

    public static List<Store> Stores(int tenantId) => new()
    {
        new Store
        {
            Id = 1,
            TenantId = tenantId,
            Name = "Taunton",
            OpenFrom = new TimeSpan(9, 0, 0),
            OpenTo = new TimeSpan(17, 30, 0),
            DaysOpen = new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday },
            HoursByDay = new Dictionary<DayOfWeek, StoreDayHours>
            {
                [DayOfWeek.Monday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Tuesday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Wednesday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Thursday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Friday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Saturday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0))
            }
        },
        new Store
        {
            Id = 2,
            TenantId = tenantId,
            Name = "Yeovil",
            OpenFrom = new TimeSpan(9, 0, 0),
            OpenTo = new TimeSpan(17, 30, 0),
            DaysOpen = new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday },
            HoursByDay = new Dictionary<DayOfWeek, StoreDayHours>
            {
                [DayOfWeek.Monday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Tuesday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Wednesday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Thursday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Friday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Saturday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0))
            }
        },
        new Store
        {
            Id = 3,
            TenantId = tenantId,
            Name = "Bristol",
            OpenFrom = new TimeSpan(9, 0, 0),
            OpenTo = new TimeSpan(17, 30, 0),
            DaysOpen = new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday },
            HoursByDay = new Dictionary<DayOfWeek, StoreDayHours>
            {
                [DayOfWeek.Monday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Tuesday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Wednesday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Thursday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Friday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Saturday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0))
            }
        },
        new Store
        {
            Id = 4,
            TenantId = tenantId,
            Name = "Bridgwater",
            OpenFrom = new TimeSpan(9, 0, 0),
            OpenTo = new TimeSpan(17, 30, 0),
            DaysOpen = new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday },
            HoursByDay = new Dictionary<DayOfWeek, StoreDayHours>
            {
                [DayOfWeek.Monday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Tuesday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Wednesday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Thursday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Friday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Saturday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0))
            }
        },
        new Store
        {
            Id = 5,
            TenantId = tenantId,
            Name = "Weston",
            OpenFrom = new TimeSpan(9, 0, 0),
            OpenTo = new TimeSpan(17, 30, 0),
            DaysOpen = new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday },
            HoursByDay = new Dictionary<DayOfWeek, StoreDayHours>
            {
                [DayOfWeek.Monday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Tuesday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Wednesday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Thursday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Friday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Saturday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0))
            }
        },
        new Store
        {
            Id = 6,
            TenantId = tenantId,
            Name = "Hereford",
            OpenFrom = new TimeSpan(9, 0, 0),
            OpenTo = new TimeSpan(17, 30, 0),
            DaysOpen = new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday },
            HoursByDay = new Dictionary<DayOfWeek, StoreDayHours>
            {
                [DayOfWeek.Monday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Tuesday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Wednesday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Thursday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Friday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Saturday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0))
            }
        },
    };

    public static List<Mechanic> Mechanics(int tenantId) => new()
    {
        new Mechanic
        {
            Id = 101,
            TenantId = tenantId,
            StoreId = 1,
            Name = "Dan",
            MaxBookableHoursPerDay = 6.0,
            DaysWorking = new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday },
            HoursByDay = new Dictionary<DayOfWeek, StoreDayHours>
            {
                [DayOfWeek.Monday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Tuesday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Wednesday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Thursday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Friday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Saturday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0))
            },
            SkillLevel = "Advanced"
        },
        new Mechanic
        {
            Id = 102,
            TenantId = tenantId,
            StoreId = 1,
            Name = "Lee",
            MaxBookableHoursPerDay = 5.5,
            DaysWorking = new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday },
            HoursByDay = new Dictionary<DayOfWeek, StoreDayHours>
            {
                [DayOfWeek.Monday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Tuesday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Wednesday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Thursday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Friday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Saturday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0))
            },
            SkillLevel = "Advanced"
        },
        new Mechanic
        {
            Id = 103,
            TenantId = tenantId,
            StoreId = 1,
            Name = "Sam",
            MaxBookableHoursPerDay = 6.0,
            DaysWorking = new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday },
            HoursByDay = new Dictionary<DayOfWeek, StoreDayHours>
            {
                [DayOfWeek.Monday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Tuesday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Wednesday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Thursday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Friday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Saturday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0))
            },
            SkillLevel = "Advanced"
        },
        new Mechanic
        {
            Id = 201,
            TenantId = tenantId,
            StoreId = 2,
            Name = "Mitch",
            MaxBookableHoursPerDay = 6.0,
            DaysWorking = new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday },
            HoursByDay = new Dictionary<DayOfWeek, StoreDayHours>
            {
                [DayOfWeek.Monday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Tuesday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Wednesday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Thursday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Friday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Saturday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0))
            },
            SkillLevel = "Advanced"
        },
        new Mechanic
        {
            Id = 202,
            TenantId = tenantId,
            StoreId = 2,
            Name = "Alex",
            MaxBookableHoursPerDay = 6.0,
            DaysWorking = new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday },
            HoursByDay = new Dictionary<DayOfWeek, StoreDayHours>
            {
                [DayOfWeek.Monday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Tuesday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Wednesday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Thursday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Friday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Saturday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0))
            },
            SkillLevel = "Advanced"
        },
        new Mechanic
        {
            Id = 301,
            TenantId = tenantId,
            StoreId = 3,
            Name = "Chris",
            MaxBookableHoursPerDay = 6.0,
            DaysWorking = new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday },
            HoursByDay = new Dictionary<DayOfWeek, StoreDayHours>
            {
                [DayOfWeek.Monday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Tuesday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Wednesday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Thursday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Friday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Saturday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0))
            },
            SkillLevel = "Advanced"
        },
        new Mechanic
        {
            Id = 302,
            TenantId = tenantId,
            StoreId = 3,
            Name = "Jamie",
            MaxBookableHoursPerDay = 6.0,
            DaysWorking = new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday },
            HoursByDay = new Dictionary<DayOfWeek, StoreDayHours>
            {
                [DayOfWeek.Monday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Tuesday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Wednesday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Thursday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Friday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Saturday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0))
            },
            SkillLevel = "Advanced"
        },
        new Mechanic
        {
            Id = 401,
            TenantId = tenantId,
            StoreId = 4,
            Name = "Taylor",
            MaxBookableHoursPerDay = 6.0,
            DaysWorking = new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday },
            HoursByDay = new Dictionary<DayOfWeek, StoreDayHours>
            {
                [DayOfWeek.Monday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Tuesday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Wednesday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Thursday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Friday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Saturday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0))
            },
            SkillLevel = "Advanced"
        },
        new Mechanic
        {
            Id = 501,
            TenantId = tenantId,
            StoreId = 5,
            Name = "Jordan",
            MaxBookableHoursPerDay = 6.0,
            DaysWorking = new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday },
            HoursByDay = new Dictionary<DayOfWeek, StoreDayHours>
            {
                [DayOfWeek.Monday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Tuesday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Wednesday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Thursday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Friday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Saturday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0))
            },
            SkillLevel = "Advanced"
        },
        new Mechanic
        {
            Id = 601,
            TenantId = tenantId,
            StoreId = 6,
            Name = "Casey",
            MaxBookableHoursPerDay = 6.0,
            DaysWorking = new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday },
            HoursByDay = new Dictionary<DayOfWeek, StoreDayHours>
            {
                [DayOfWeek.Monday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Tuesday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Wednesday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Thursday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Friday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0)),
                [DayOfWeek.Saturday] = new StoreDayHours(new TimeSpan(9, 0, 0), new TimeSpan(17, 30, 0))
            },
            SkillLevel = "Advanced"
        },
    };

    public static List<Booking> Bookings(int tenantId) => new()
    {
        new Booking
        {
            Id = 1,
            TenantId = tenantId,
            StoreId = 1,
            MechanicId = 101,
            Title = "Bronze service",
            Start = DateTime.Today.AddHours(9),
            End = DateTime.Today.AddHours(10),
            JobId = "SVC_BRONZE",
            JobIds = new[] { "SVC_BRONZE" },
            AddOnIds = Array.Empty<string>(),
            TotalMinutes = 60,
            TotalPriceIncVat = 79.00m,
            StatusName = "Scheduled"
        },
        new Booking
        {
            Id = 2,
            TenantId = tenantId,
            StoreId = 1,
            MechanicId = 102,
            Title = "Gold service",
            Start = DateTime.Today.AddHours(11),
            End = DateTime.Today.AddHours(12).AddMinutes(30),
            JobId = "SVC_GOLD",
            JobIds = new[] { "SVC_GOLD" },
            AddOnIds = Array.Empty<string>(),
            TotalMinutes = 90,
            TotalPriceIncVat = 149.00m,
            StatusName = "Scheduled"
        }
    };

    public static List<BookingStatus> BookingStatuses(int tenantId) => new()
    {
        new BookingStatus { TenantId = tenantId, Name = "Scheduled", ColorHex = "#3b82f6" },
        new BookingStatus { TenantId = tenantId, Name = "Arrived", ColorHex = "#10b981" },
        new BookingStatus { TenantId = tenantId, Name = "In Progress", ColorHex = "#f59e0b" },
        new BookingStatus { TenantId = tenantId, Name = "Completed", ColorHex = "#22c55e" },
        new BookingStatus { TenantId = tenantId, Name = "Assessment/Quote", ColorHex = "#0ea5e9" },
        new BookingStatus { TenantId = tenantId, Name = "Delayed", ColorHex = "#ef4444" },
        new BookingStatus { TenantId = tenantId, Name = "No Show", ColorHex = "#6b7280" },
        new BookingStatus { TenantId = tenantId, Name = "Waiting Customer", ColorHex = "#a855f7" },
        new BookingStatus { TenantId = tenantId, Name = "Waiting Parts", ColorHex = "#f97316" },
        new BookingStatus { TenantId = tenantId, Name = "Waiting Warranty", ColorHex = "#14b8a6" }
    };

    public static List<GlobalServiceCategory> GlobalServiceCategories() => new()
    {
        new() { Category1 = "Accessories", Category2 = "", ColorHex = "#22c55e", SortOrder = 1 },
        new() { Category1 = "Accessories", Category2 = "Fitting Accessories", ColorHex = "#22c55e", SortOrder = 2 },
        new() { Category1 = "Brake", Category2 = "", ColorHex = "#ef4444", SortOrder = 3 },
        new() { Category1 = "Brake", Category2 = "Hydraulic", ColorHex = "#ef4444", SortOrder = 4 },
        new() { Category1 = "Gear", Category2 = "", ColorHex = "#f59e0b", SortOrder = 5 },
        new() { Category1 = "Gear", Category2 = "Drivetrain", ColorHex = "#f59e0b", SortOrder = 6 },
        new() { Category1 = "Wheel", Category2 = "", ColorHex = "#10b981", SortOrder = 7 },
        new() { Category1 = "Wheel", Category2 = "Puncture / Tyre", ColorHex = "#10b981", SortOrder = 8 },
        new() { Category1 = "Wheel", Category2 = "Wheel True", ColorHex = "#10b981", SortOrder = 9 },
        new() { Category1 = "Wheel", Category2 = "Tubeless", ColorHex = "#10b981", SortOrder = 10 },
        new() { Category1 = "Suspension", Category2 = "", ColorHex = "#0ea5e9", SortOrder = 11 },
        new() { Category1 = "Suspension", Category2 = "Service", ColorHex = "#0ea5e9", SortOrder = 12 },
        new() { Category1 = "Frame", Category2 = "", ColorHex = "#8b5cf6", SortOrder = 13 },
        new() { Category1 = "Frame", Category2 = "Repair", ColorHex = "#8b5cf6", SortOrder = 14 },
        new() { Category1 = "E-Bike", Category2 = "", ColorHex = "#14b8a6", SortOrder = 15 },
        new() { Category1 = "E-Bike", Category2 = "Diagnostics", ColorHex = "#14b8a6", SortOrder = 16 }
    };

    public static List<GlobalServiceTemplate> GlobalServiceTemplates() => new()
    {
        new() { Name = "Bar Tape", Category1 = "Accessories", Category2 = "Fitting Accessories", DefaultMinutes = 15, BasePriceIncVat = 19.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 1 },
        new() { Name = "Fitting Basket - Bar mounted", Category1 = "Accessories", Category2 = "Fitting Accessories", DefaultMinutes = 20, BasePriceIncVat = 25.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 2 },
        new() { Name = "Fitting Computer", Category1 = "Accessories", Category2 = "Fitting Accessories", DefaultMinutes = 15, BasePriceIncVat = 19.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 3 },
        new() { Name = "Fitting Grips", Category1 = "Accessories", Category2 = "Fitting Accessories", DefaultMinutes = 5, BasePriceIncVat = 5.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Discounted, SortOrder = 4 },
        new() { Name = "Brake bleed", Category1 = "Brake", Category2 = "Hydraulic", DefaultMinutes = 30, BasePriceIncVat = 35.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 5 },
        new() { Name = "Replace chain", Category1 = "Gear", Category2 = "Drivetrain", DefaultMinutes = 20, BasePriceIncVat = 29.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 6 },
        new() { Name = "Puncture / Tyre Fit / Tubeless - Wheel on bike", Category1 = "Wheel", Category2 = "Puncture / Tyre", DefaultMinutes = 15, BasePriceIncVat = 15.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Discounted, SortOrder = 7 },
        new() { Name = "Puncture / Tyre Fit / Tubeless - Wheel only", Category1 = "Wheel", Category2 = "Puncture / Tyre", DefaultMinutes = 12, BasePriceIncVat = 10.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.LossLeader, SortOrder = 8 },
        new() { Name = "Spoke and true", Category1 = "Wheel", Category2 = "Wheel True", DefaultMinutes = 45, BasePriceIncVat = 57.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 9 },
        new() { Name = "Tubeless sealant top up - Per wheel", Category1 = "Wheel", Category2 = "Tubeless", DefaultMinutes = 5, BasePriceIncVat = 5.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Discounted, SortOrder = 10 },
        new() { Name = "Software update", Category1 = "E-Bike", Category2 = "Diagnostics", DefaultMinutes = 45, BasePriceIncVat = 59.00m, PricingMode = ServicePricingMode.EstimatedPrice, EstimatedPriceIncVat = 59.00m, SortOrder = 11 }
    };
}
