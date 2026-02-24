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
                ["ACCESSORIES"] = "#22c55e",
                ["LABOUR"] = "#f97316",
                ["WARRANTY"] = "#64748b",
                ["E-BIKE SERVICING"] = "#14b8a6",
                ["FRAME"] = "#8b5cf6",
                ["SUSPENSION"] = "#0ea5e9",
                ["WHEELS"] = "#10b981",
                ["GEAR"] = "#f59e0b",
                ["BRAKES"] = "#ef4444"
            },
            ServiceCategoryHierarchy = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Service Packages"] = new List<string>(),
                ["ACCESSORIES"] = new List<string> { "FITTING ACCESSORIES" },
                ["LABOUR"] = new List<string> { "FREE", "CHARGEABLE", "LABOUR" },
                ["WARRANTY"] = new List<string> { "LABOUR", "SEND EXTERNAL", "WARRANTY SERVICING" },
                ["E-BIKE SERVICING"] = new List<string> { "ELECTRIC BIKE", "SEND EXTERNAL" },
                ["FRAME"] = new List<string>
                {
                    "HEADSET",
                    "BOTTOM BRACKET",
                    "CLEAN BIKE",
                    "FRAME PROTECTION",
                    "FRAME PREP / REPAIR",
                    "BIKE BUILD",
                    "DROPPER POST",
                    "SEND EXTERNAL",
                    "SEND EXTERNAL FOR REPAIR / SERVICE / TUNE"
                },
                ["SUSPENSION"] = new List<string>
                {
                    "SETUP",
                    "FORK",
                    "SHOCK",
                    "FRAME BEARINGS",
                    "SEND EXTERNAL FOR REPAIR / SERVICE / TUNE"
                },
                ["WHEELS"] = new List<string>
                {
                    "PUNCTURE / TYRE",
                    "WHEEL FIT",
                    "WHEEL TRUE",
                    "WHEEL BUILD",
                    "TUBELESS / TUBULAR",
                    "HUB / FREEHUB"
                },
                ["GEAR"] = new List<string>
                {
                    "ADJUST",
                    "CABLES",
                    "DRIVE TRAIN",
                    "MECHS OR DERAILLEURS",
                    "SHIFTERS",
                    "GROUPSET"
                },
                ["BRAKES"] = new List<string>
                {
                    "ADJUST",
                    "CABLES",
                    "PADS AND ROTORS",
                    "CALIPERS",
                    "LEVERS",
                    "FULL SYSTEM",
                    "BRAKE BLEED"
                }
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
            Category = "GEAR",
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
            Category = "BRAKES",
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
            Category = "E-BIKE SERVICING",
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
            Category = "ACCESSORIES",
            Description = ""
        },
        new AddOnDefinition
        {
            Id = "ADD_BLEED",
            TenantId = tenantId,
            Name = "Brake bleed",
            Category = "BRAKES",
            Description = ""
        },
        new AddOnDefinition
        {
            Id = "ADD_TUBE",
            TenantId = tenantId,
            Name = "Replace inner tube",
            Category = "WHEELS",
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
        new() { Category1 = "Service Packages", Category2 = "", ColorHex = "#4f46e5", SortOrder = 1 },
        new() { Category1 = "ACCESSORIES", Category2 = "", ColorHex = "#22c55e", SortOrder = 2 },
        new() { Category1 = "ACCESSORIES", Category2 = "FITTING ACCESSORIES", ColorHex = "#22c55e", SortOrder = 3 },
        new() { Category1 = "LABOUR", Category2 = "", ColorHex = "#f97316", SortOrder = 4 },
        new() { Category1 = "LABOUR", Category2 = "FREE", ColorHex = "#f97316", SortOrder = 5 },
        new() { Category1 = "LABOUR", Category2 = "CHARGEABLE", ColorHex = "#f97316", SortOrder = 6 },
        new() { Category1 = "WARRANTY", Category2 = "", ColorHex = "#64748b", SortOrder = 7 },
        new() { Category1 = "WARRANTY", Category2 = "LABOUR", ColorHex = "#64748b", SortOrder = 8 },
        new() { Category1 = "WARRANTY", Category2 = "SEND EXTERNAL", ColorHex = "#64748b", SortOrder = 9 },
        new() { Category1 = "E-BIKE SERVICING", Category2 = "", ColorHex = "#14b8a6", SortOrder = 10 },
        new() { Category1 = "E-BIKE SERVICING", Category2 = "ELECTRIC BIKE", ColorHex = "#14b8a6", SortOrder = 11 },
        new() { Category1 = "E-BIKE SERVICING", Category2 = "SEND EXTERNAL", ColorHex = "#14b8a6", SortOrder = 12 },
        new() { Category1 = "FRAME", Category2 = "", ColorHex = "#8b5cf6", SortOrder = 13 },
        new() { Category1 = "FRAME", Category2 = "HEADSET", ColorHex = "#8b5cf6", SortOrder = 14 },
        new() { Category1 = "FRAME", Category2 = "BOTTOM BRACKET", ColorHex = "#8b5cf6", SortOrder = 15 },
        new() { Category1 = "FRAME", Category2 = "CLEAN BIKE", ColorHex = "#8b5cf6", SortOrder = 16 },
        new() { Category1 = "FRAME", Category2 = "FRAME PROTECTION", ColorHex = "#8b5cf6", SortOrder = 17 },
        new() { Category1 = "FRAME", Category2 = "FRAME PREP / REPAIR", ColorHex = "#8b5cf6", SortOrder = 18 },
        new() { Category1 = "FRAME", Category2 = "BIKE BUILD", ColorHex = "#8b5cf6", SortOrder = 19 },
        new() { Category1 = "FRAME", Category2 = "DROPPER POST", ColorHex = "#8b5cf6", SortOrder = 20 },
        new() { Category1 = "FRAME", Category2 = "SEND EXTERNAL", ColorHex = "#8b5cf6", SortOrder = 21 },
        new() { Category1 = "SUSPENSION", Category2 = "", ColorHex = "#0ea5e9", SortOrder = 22 },
        new() { Category1 = "SUSPENSION", Category2 = "SETUP", ColorHex = "#0ea5e9", SortOrder = 23 },
        new() { Category1 = "SUSPENSION", Category2 = "FORK", ColorHex = "#0ea5e9", SortOrder = 24 },
        new() { Category1 = "SUSPENSION", Category2 = "SHOCK", ColorHex = "#0ea5e9", SortOrder = 25 },
        new() { Category1 = "SUSPENSION", Category2 = "FRAME BEARINGS", ColorHex = "#0ea5e9", SortOrder = 26 },
        new() { Category1 = "SUSPENSION", Category2 = "SEND EXTERNAL FOR REPAIR / SERVICE / TUNE", ColorHex = "#0ea5e9", SortOrder = 27 },
        new() { Category1 = "WHEELS", Category2 = "", ColorHex = "#10b981", SortOrder = 28 },
        new() { Category1 = "WHEELS", Category2 = "PUNCTURE / TYRE", ColorHex = "#10b981", SortOrder = 29 },
        new() { Category1 = "WHEELS", Category2 = "WHEEL FIT", ColorHex = "#10b981", SortOrder = 30 },
        new() { Category1 = "WHEELS", Category2 = "WHEEL TRUE", ColorHex = "#10b981", SortOrder = 31 },
        new() { Category1 = "WHEELS", Category2 = "WHEEL BUILD", ColorHex = "#10b981", SortOrder = 32 },
        new() { Category1 = "WHEELS", Category2 = "TUBELESS / TUBULAR", ColorHex = "#10b981", SortOrder = 33 },
        new() { Category1 = "WHEELS", Category2 = "HUB / FREEHUB", ColorHex = "#10b981", SortOrder = 34 },
        new() { Category1 = "GEAR", Category2 = "", ColorHex = "#f59e0b", SortOrder = 35 },
        new() { Category1 = "GEAR", Category2 = "ADJUST", ColorHex = "#f59e0b", SortOrder = 36 },
        new() { Category1 = "GEAR", Category2 = "CABLES", ColorHex = "#f59e0b", SortOrder = 37 },
        new() { Category1 = "GEAR", Category2 = "DRIVE TRAIN", ColorHex = "#f59e0b", SortOrder = 38 },
        new() { Category1 = "GEAR", Category2 = "MECHS OR DERAILLEURS", ColorHex = "#f59e0b", SortOrder = 39 },
        new() { Category1 = "GEAR", Category2 = "SHIFTERS", ColorHex = "#f59e0b", SortOrder = 40 },
        new() { Category1 = "GEAR", Category2 = "GROUPSET", ColorHex = "#f59e0b", SortOrder = 41 },
        new() { Category1 = "BRAKES", Category2 = "", ColorHex = "#ef4444", SortOrder = 42 },
        new() { Category1 = "BRAKES", Category2 = "ADJUST", ColorHex = "#ef4444", SortOrder = 43 },
        new() { Category1 = "BRAKES", Category2 = "CABLES", ColorHex = "#ef4444", SortOrder = 44 },
        new() { Category1 = "BRAKES", Category2 = "PADS AND ROTORS", ColorHex = "#ef4444", SortOrder = 45 },
        new() { Category1 = "BRAKES", Category2 = "CALIPERS", ColorHex = "#ef4444", SortOrder = 46 },
        new() { Category1 = "BRAKES", Category2 = "LEVERS", ColorHex = "#ef4444", SortOrder = 47 },
        new() { Category1 = "BRAKES", Category2 = "FULL SYSTEM", ColorHex = "#ef4444", SortOrder = 48 },
        new() { Category1 = "BRAKES", Category2 = "BRAKE BLEED", ColorHex = "#ef4444", SortOrder = 49 },
        new() { Category1 = "LABOUR", Category2 = "LABOUR", ColorHex = "#f97316", SortOrder = 50 },
        new() { Category1 = "WARRANTY", Category2 = "WARRANTY SERVICING", ColorHex = "#64748b", SortOrder = 51 },
        new() { Category1 = "FRAME", Category2 = "SEND EXTERNAL FOR REPAIR / SERVICE / TUNE", ColorHex = "#8b5cf6", SortOrder = 52 }
    };

    public static List<GlobalServiceTemplate> GlobalServiceTemplates() => new()
    {
        new() { Name = "Puncture / Tyre Fit / Tubeless - Wheel only", Category1 = "WHEELS", Category2 = "PUNCTURE / TYRE", DefaultMinutes = 12, BasePriceIncVat = 10.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.LossLeader, SortOrder = 1 },
        new() { Name = "Fitting Grips", Category1 = "ACCESSORIES", Category2 = "FITTING ACCESSORIES", DefaultMinutes = 5, BasePriceIncVat = 5.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Discounted, SortOrder = 2 },
        new() { Name = "Fitting Pedals", Category1 = "ACCESSORIES", Category2 = "FITTING ACCESSORIES", DefaultMinutes = 5, BasePriceIncVat = 5.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Discounted, SortOrder = 3 },
        new() { Name = "Fitting Saddle", Category1 = "ACCESSORIES", Category2 = "FITTING ACCESSORIES", DefaultMinutes = 5, BasePriceIncVat = 5.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Discounted, SortOrder = 4 },
        new() { Name = "Puncture / Tyre Fit / Tubeless - Wheel on bike", Category1 = "WHEELS", Category2 = "PUNCTURE / TYRE", DefaultMinutes = 15, BasePriceIncVat = 15.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Discounted, SortOrder = 5 },
        new() { Name = "Puncture / Tyre - Hub gear / brake bike / electric hub", Category1 = "WHEELS", Category2 = "PUNCTURE / TYRE", DefaultMinutes = 30, BasePriceIncVat = 30.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Discounted, SortOrder = 6 },
        new() { Name = "Tubeless sealant top up - Per wheel", Category1 = "WHEELS", Category2 = "TUBELESS / TUBULAR", DefaultMinutes = 5, BasePriceIncVat = 5.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Discounted, SortOrder = 7 },
        new() { Name = "Spoke and true - Additional spoke each", Category1 = "WHEELS", Category2 = "WHEEL TRUE", DefaultMinutes = 3, BasePriceIncVat = 3.80m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 8 },
        new() { Name = "Fit gear cable - single (Inner and Outer)", Category1 = "GEAR", Category2 = "CABLES", DefaultMinutes = 25, BasePriceIncVat = 31.67m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 9 },
        new() { Name = "Fit chain and cassette", Category1 = "GEAR", Category2 = "DRIVE TRAIN", DefaultMinutes = 25, BasePriceIncVat = 31.67m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 10 },
        new() { Name = "Fit outer and inner cable / internally routed", Category1 = "BRAKES", Category2 = "CABLES", DefaultMinutes = 25, BasePriceIncVat = 31.67m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 11 },
        new() { Name = "Fit new fork / cut steerer tube", Category1 = "SUSPENSION", Category2 = "FORK", DefaultMinutes = 45, BasePriceIncVat = 57.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 12 },
        new() { Name = "Fitting Basket - Bar mounted", Category1 = "ACCESSORIES", Category2 = "FITTING ACCESSORIES", DefaultMinutes = 20, BasePriceIncVat = 25.33m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 13 },
        new() { Name = "LABOUR - CHARGEABLE 15 MINUTES", Category1 = "LABOUR", Category2 = "LABOUR", DefaultMinutes = 15, BasePriceIncVat = 19.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 14 },
        new() { Name = "Electric Bike Motor Removal and Fitting", Category1 = "E-BIKE SERVICING", Category2 = "ELECTRIC BIKE", DefaultMinutes = 60, BasePriceIncVat = 76.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 15 },
        new() { Name = "Fit specialist headset - INTERNAL hose, including brake bleed", Category1 = "FRAME", Category2 = "HEADSET", DefaultMinutes = 60, BasePriceIncVat = 76.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 16 },
        new() { Name = "INVISI FRAME FITTING", Category1 = "FRAME", Category2 = "FRAME PROTECTION", DefaultMinutes = 110, BasePriceIncVat = 139.33m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 17 },
        new() { Name = "INVISI FRAME + FORK FITTING", Category1 = "FRAME", Category2 = "FRAME PROTECTION", DefaultMinutes = 140, BasePriceIncVat = 177.33m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 18 },
        new() { Name = "Bare frame prep - Tap, face and ream", Category1 = "FRAME", Category2 = "FRAME PREP / REPAIR", DefaultMinutes = 60, BasePriceIncVat = 76.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 19 },
        new() { Name = "Tap & Helicoil Crank", Category1 = "FRAME", Category2 = "FRAME PREP / REPAIR", DefaultMinutes = 25, BasePriceIncVat = 31.67m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 20 },
        new() { Name = "PDI boxed bike - Road / MTB / Hybrid ( per hour )", Category1 = "FRAME", Category2 = "BIKE BUILD", DefaultMinutes = 60, BasePriceIncVat = 76.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 21 },
        new() { Name = "Frame Swap", Category1 = "FRAME", Category2 = "BIKE BUILD", DefaultMinutes = 120, BasePriceIncVat = 152.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 22 },
        new() { Name = "Dropper Post Service Hydraulic", Category1 = "FRAME", Category2 = "DROPPER POST", DefaultMinutes = 60, BasePriceIncVat = 76.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 23 },
        new() { Name = "Basic suspension setup - not one of our brands", Category1 = "SUSPENSION", Category2 = "SETUP", DefaultMinutes = 8, BasePriceIncVat = 10.13m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 24 },
        new() { Name = "Fit new fork / cut steerer tube - INTERNAL hose plus bleed", Category1 = "SUSPENSION", Category2 = "FORK", DefaultMinutes = 60, BasePriceIncVat = 76.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 25 },
        new() { Name = "Remove + fit rear shock / fit new bushes", Category1 = "SUSPENSION", Category2 = "SHOCK", DefaultMinutes = 20, BasePriceIncVat = 25.33m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 26 },
        new() { Name = "Basic air can service", Category1 = "SUSPENSION", Category2 = "SHOCK", DefaultMinutes = 20, BasePriceIncVat = 25.33m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 27 },
        new() { Name = "Shock Bush Fit", Category1 = "SUSPENSION", Category2 = "FRAME BEARINGS", DefaultMinutes = 20, BasePriceIncVat = 25.33m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 28 },
        new() { Name = "Fit front wheel", Category1 = "WHEELS", Category2 = "WHEEL FIT", DefaultMinutes = 20, BasePriceIncVat = 25.33m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 29 },
        new() { Name = "Spoke and true - Wheel on bike", Category1 = "WHEELS", Category2 = "WHEEL TRUE", DefaultMinutes = 20, BasePriceIncVat = 25.33m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 30 },
        new() { Name = "Tubeless conversion - NON tubeless ready", Category1 = "WHEELS", Category2 = "TUBELESS / TUBULAR", DefaultMinutes = 20, BasePriceIncVat = 25.33m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 31 },
        new() { Name = "Fit tubular tyre ( Glued )", Category1 = "WHEELS", Category2 = "TUBELESS / TUBULAR", DefaultMinutes = 60, BasePriceIncVat = 76.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 32 },
        new() { Name = "Fit gear cable - pair (Inner and Outer)", Category1 = "GEAR", Category2 = "CABLES", DefaultMinutes = 40, BasePriceIncVat = 50.67m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 33 },
        new() { Name = "Fit gear shifter - single", Category1 = "GEAR", Category2 = "SHIFTERS", DefaultMinutes = 20, BasePriceIncVat = 25.33m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 34 },
        new() { Name = "Fit gear shifter - pair", Category1 = "GEAR", Category2 = "SHIFTERS", DefaultMinutes = 40, BasePriceIncVat = 50.67m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 35 },
        new() { Name = "Fit road brake lever - pair (bar tape, cables, gear / brake adjust, bleed if required)", Category1 = "GEAR", Category2 = "SHIFTERS", DefaultMinutes = 60, BasePriceIncVat = 76.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 36 },
        new() { Name = "Remove groupset", Category1 = "GEAR", Category2 = "GROUPSET", DefaultMinutes = 20, BasePriceIncVat = 25.33m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 37 },
        new() { Name = "Groupset fit - Mech / Mech Hydro / Mech Di2", Category1 = "GEAR", Category2 = "GROUPSET", DefaultMinutes = 100, BasePriceIncVat = 126.67m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 38 },
        new() { Name = "Groupset fit - Di2 / Hydro", Category1 = "GEAR", Category2 = "GROUPSET", DefaultMinutes = 130, BasePriceIncVat = 164.67m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 39 },
        new() { Name = "Fit inner brake cable", Category1 = "BRAKES", Category2 = "CABLES", DefaultMinutes = 20, BasePriceIncVat = 25.33m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 40 },
        new() { Name = "Fit outer and inner cable / internally routed - pair", Category1 = "BRAKES", Category2 = "CABLES", DefaultMinutes = 40, BasePriceIncVat = 50.67m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 41 },
        new() { Name = "Fit disc rotor - includes fitting pads if required", Category1 = "BRAKES", Category2 = "PADS AND ROTORS", DefaultMinutes = 20, BasePriceIncVat = 25.33m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 42 },
        new() { Name = "Fit caliper", Category1 = "BRAKES", Category2 = "CALIPERS", DefaultMinutes = 20, BasePriceIncVat = 25.33m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 43 },
        new() { Name = "Fit road brake lever - pair (bar tape, cables, gear / brake adjust, bleed if required)", Category1 = "BRAKES", Category2 = "LEVERS", DefaultMinutes = 60, BasePriceIncVat = 76.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 44 },
        new() { Name = "Dropper Post Fit for E-Bikes", Category1 = "FRAME", Category2 = "DROPPER POST", DefaultMinutes = 75, BasePriceIncVat = 95.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 45 },
        new() { Name = "Wheel Build", Category1 = "WHEELS", Category2 = "WHEEL BUILD", DefaultMinutes = 75, BasePriceIncVat = 95.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 46 },
        new() { Name = "Fitting Handlebars - Road, includes bar tape fitting - INTERNAL cable / hose", Category1 = "ACCESSORIES", Category2 = "FITTING ACCESSORIES", DefaultMinutes = 90, BasePriceIncVat = 114.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 47 },
        new() { Name = "Sturmey hub service", Category1 = "WHEELS", Category2 = "HUB / FREEHUB", DefaultMinutes = 90, BasePriceIncVat = 114.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 48 },
        new() { Name = "Tyre insert fit - Per wheel", Category1 = "WHEELS", Category2 = "PUNCTURE / TYRE", DefaultMinutes = 35, BasePriceIncVat = 44.33m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 49 },
        new() { Name = "Spoke and true - Concealed nipple or Tubeless", Category1 = "WHEELS", Category2 = "WHEEL TRUE", DefaultMinutes = 35, BasePriceIncVat = 44.33m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 50 },
        new() { Name = "Rear hub service", Category1 = "WHEELS", Category2 = "HUB / FREEHUB", DefaultMinutes = 35, BasePriceIncVat = 44.33m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 51 },
        new() { Name = "Freehub body replacement", Category1 = "WHEELS", Category2 = "HUB / FREEHUB", DefaultMinutes = 35, BasePriceIncVat = 44.33m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 52 },
        new() { Name = "Fit gear cable - road bike single - including fitting new bar tape", Category1 = "GEAR", Category2 = "CABLES", DefaultMinutes = 35, BasePriceIncVat = 44.33m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 53 },
        new() { Name = "Fit mech hanger and rear mech", Category1 = "GEAR", Category2 = "MECHS OR DERAILLEURS", DefaultMinutes = 35, BasePriceIncVat = 44.33m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 54 },
        new() { Name = "Fit cable to road bike - including fitting new bar tape", Category1 = "BRAKES", Category2 = "CABLES", DefaultMinutes = 35, BasePriceIncVat = 44.33m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 55 },
        new() { Name = "Fit hydraulic caliper - including bleed", Category1 = "BRAKES", Category2 = "CALIPERS", DefaultMinutes = 35, BasePriceIncVat = 44.33m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 56 },
        new() { Name = "Fit hydraulic brake lever - including bleed", Category1 = "BRAKES", Category2 = "LEVERS", DefaultMinutes = 35, BasePriceIncVat = 44.33m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 57 },
        new() { Name = "Fit MTB hydraulic lever, hose and caliper - no bleed required", Category1 = "BRAKES", Category2 = "FULL SYSTEM", DefaultMinutes = 35, BasePriceIncVat = 44.33m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 58 },
        new() { Name = "Strip and replace frame bearings ( 2.5 hours )", Category1 = "SUSPENSION", Category2 = "FRAME BEARINGS", DefaultMinutes = 150, BasePriceIncVat = 190.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 59 },
        new() { Name = "Strip frame, clean / prep and build bike", Category1 = "FRAME", Category2 = "BIKE BUILD", DefaultMinutes = 180, BasePriceIncVat = 228.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 60 },
        new() { Name = "Strip and replace frame bearings ( 3 hours )", Category1 = "SUSPENSION", Category2 = "FRAME BEARINGS", DefaultMinutes = 180, BasePriceIncVat = 228.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 61 },
        new() { Name = "Bar Tape", Category1 = "ACCESSORIES", Category2 = "FITTING ACCESSORIES", DefaultMinutes = 15, BasePriceIncVat = 19.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 62 },
        new() { Name = "Fitting Computer", Category1 = "ACCESSORIES", Category2 = "FITTING ACCESSORIES", DefaultMinutes = 15, BasePriceIncVat = 19.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 63 },
        new() { Name = "Fitting Handlebars - Flat", Category1 = "ACCESSORIES", Category2 = "FITTING ACCESSORIES", DefaultMinutes = 15, BasePriceIncVat = 19.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 64 },
        new() { Name = "Fitting Handlebars - Road, includes bar tape fitting", Category1 = "ACCESSORIES", Category2 = "FITTING ACCESSORIES", DefaultMinutes = 30, BasePriceIncVat = 38.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 65 },
        new() { Name = "Fitting Mudguards - Clip on", Category1 = "ACCESSORIES", Category2 = "FITTING ACCESSORIES", DefaultMinutes = 15, BasePriceIncVat = 19.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 66 },
        new() { Name = "Fitting Mudguards - Full", Category1 = "ACCESSORIES", Category2 = "FITTING ACCESSORIES", DefaultMinutes = 30, BasePriceIncVat = 38.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 67 },
        new() { Name = "Fitting Pannier Rack", Category1 = "ACCESSORIES", Category2 = "FITTING ACCESSORIES", DefaultMinutes = 15, BasePriceIncVat = 19.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 68 },
        new() { Name = "Fitting Stabilisers", Category1 = "ACCESSORIES", Category2 = "FITTING ACCESSORIES", DefaultMinutes = 15, BasePriceIncVat = 19.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 69 },
        new() { Name = "Fitting Stem", Category1 = "ACCESSORIES", Category2 = "FITTING ACCESSORIES", DefaultMinutes = 15, BasePriceIncVat = 19.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 70 },
        new() { Name = "Fitting TCU", Category1 = "ACCESSORIES", Category2 = "FITTING ACCESSORIES", DefaultMinutes = 15, BasePriceIncVat = 19.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 71 },
        new() { Name = "BOSCH Firmware Update / Diagnostics", Category1 = "E-BIKE SERVICING", Category2 = "ELECTRIC BIKE", DefaultMinutes = 15, BasePriceIncVat = 19.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 72 },
        new() { Name = "Electric Bike diagnosis / repair ( per 15 mins )", Category1 = "E-BIKE SERVICING", Category2 = "ELECTRIC BIKE", DefaultMinutes = 15, BasePriceIncVat = 19.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 73 },
        new() { Name = "Electric Bike Motor Removal Only", Category1 = "E-BIKE SERVICING", Category2 = "ELECTRIC BIKE", DefaultMinutes = 30, BasePriceIncVat = 38.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 74 },
        new() { Name = "Adjust headset", Category1 = "FRAME", Category2 = "HEADSET", DefaultMinutes = 15, BasePriceIncVat = 19.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 75 },
        new() { Name = "Fit headset", Category1 = "FRAME", Category2 = "HEADSET", DefaultMinutes = 30, BasePriceIncVat = 38.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 76 },
        new() { Name = "Fit bottom bracket", Category1 = "FRAME", Category2 = "BOTTOM BRACKET", DefaultMinutes = 30, BasePriceIncVat = 38.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 77 },
        new() { Name = "Fit bottom bracket and tap and face", Category1 = "FRAME", Category2 = "BOTTOM BRACKET", DefaultMinutes = 45, BasePriceIncVat = 57.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 78 },
        new() { Name = "Clean bike with Muc Off / Scrub / Hose down", Category1 = "FRAME", Category2 = "CLEAN BIKE", DefaultMinutes = 15, BasePriceIncVat = 19.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 79 },
        new() { Name = "INVISI FORK FITTING", Category1 = "FRAME", Category2 = "FRAME PROTECTION", DefaultMinutes = 30, BasePriceIncVat = 38.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 80 },
        new() { Name = "Crash inspection report", Category1 = "FRAME", Category2 = "FRAME PREP / REPAIR", DefaultMinutes = 45, BasePriceIncVat = 57.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 81 },
        new() { Name = "Bottle cage boss repair - First boss", Category1 = "FRAME", Category2 = "FRAME PREP / REPAIR", DefaultMinutes = 30, BasePriceIncVat = 38.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 82 },
        new() { Name = "Additional bottle cage boss repairs", Category1 = "FRAME", Category2 = "FRAME PREP / REPAIR", DefaultMinutes = 15, BasePriceIncVat = 19.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 83 },
        new() { Name = "Remove stuck seat post ( per 15 mins )", Category1 = "FRAME", Category2 = "FRAME PREP / REPAIR", DefaultMinutes = 15, BasePriceIncVat = 19.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 84 },
        new() { Name = "Face brake caliper mount", Category1 = "FRAME", Category2 = "FRAME PREP / REPAIR", DefaultMinutes = 30, BasePriceIncVat = 38.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 85 },
        new() { Name = "PDI boxed bike - Kids / BMX", Category1 = "FRAME", Category2 = "BIKE BUILD", DefaultMinutes = 45, BasePriceIncVat = 57.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 86 },
        new() { Name = "Dropper Post Fit EXTERNAL", Category1 = "FRAME", Category2 = "DROPPER POST", DefaultMinutes = 30, BasePriceIncVat = 38.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 87 },
        new() { Name = "Dropper Post Fit INTERNAL", Category1 = "FRAME", Category2 = "DROPPER POST", DefaultMinutes = 45, BasePriceIncVat = 57.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 88 },
        new() { Name = "Dropper Post Service Mechanical", Category1 = "FRAME", Category2 = "DROPPER POST", DefaultMinutes = 30, BasePriceIncVat = 38.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 89 },
        new() { Name = "Lower leg service", Category1 = "SUSPENSION", Category2 = "FORK", DefaultMinutes = 30, BasePriceIncVat = 38.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 90 },
        new() { Name = "Strip and replace frame bearings ( 2 hours )", Category1 = "SUSPENSION", Category2 = "FRAME BEARINGS", DefaultMinutes = 120, BasePriceIncVat = 152.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 91 },
        new() { Name = "Fit rear wheel", Category1 = "WHEELS", Category2 = "WHEEL FIT", DefaultMinutes = 30, BasePriceIncVat = 38.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 92 },
        new() { Name = "True wheel", Category1 = "WHEELS", Category2 = "WHEEL TRUE", DefaultMinutes = 15, BasePriceIncVat = 19.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 93 },
        new() { Name = "True Concealed nipple or Tubeless", Category1 = "WHEELS", Category2 = "WHEEL TRUE", DefaultMinutes = 30, BasePriceIncVat = 38.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 94 },
        new() { Name = "Spoke and true - Loose wheel", Category1 = "WHEELS", Category2 = "WHEEL TRUE", DefaultMinutes = 15, BasePriceIncVat = 19.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 95 },
        new() { Name = "Tubeless conversion - Tubeless ready", Category1 = "WHEELS", Category2 = "TUBELESS / TUBULAR", DefaultMinutes = 15, BasePriceIncVat = 19.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 96 },
        new() { Name = "Fit tubular tyre ( Taped )", Category1 = "WHEELS", Category2 = "TUBELESS / TUBULAR", DefaultMinutes = 30, BasePriceIncVat = 38.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 97 },
        new() { Name = "Glue cleaning per 15 mins", Category1 = "WHEELS", Category2 = "TUBELESS / TUBULAR", DefaultMinutes = 15, BasePriceIncVat = 19.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 98 },
        new() { Name = "Front hub service", Category1 = "WHEELS", Category2 = "HUB / FREEHUB", DefaultMinutes = 30, BasePriceIncVat = 38.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 99 },
        new() { Name = "Adjust gear only", Category1 = "GEAR", Category2 = "ADJUST", DefaultMinutes = 15, BasePriceIncVat = 19.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 100 },
        new() { Name = "Di2 update and adjust - pair", Category1 = "GEAR", Category2 = "ADJUST", DefaultMinutes = 30, BasePriceIncVat = 38.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 101 },
        new() { Name = "Fit gear cable - road bike pair - including fitting new bar tape", Category1 = "GEAR", Category2 = "CABLES", DefaultMinutes = 45, BasePriceIncVat = 57.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 102 },
        new() { Name = "Fit chain", Category1 = "GEAR", Category2 = "DRIVE TRAIN", DefaultMinutes = 15, BasePriceIncVat = 19.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 103 },
        new() { Name = "Fit cassette", Category1 = "GEAR", Category2 = "DRIVE TRAIN", DefaultMinutes = 15, BasePriceIncVat = 19.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 104 },
        new() { Name = "Fit chainset", Category1 = "GEAR", Category2 = "DRIVE TRAIN", DefaultMinutes = 30, BasePriceIncVat = 38.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 105 },
        new() { Name = "Single ring conversion", Category1 = "GEAR", Category2 = "DRIVE TRAIN", DefaultMinutes = 30, BasePriceIncVat = 38.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 106 },
        new() { Name = "Fit chain device", Category1 = "GEAR", Category2 = "DRIVE TRAIN", DefaultMinutes = 30, BasePriceIncVat = 38.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 107 },
        new() { Name = "Fit mech", Category1 = "GEAR", Category2 = "MECHS OR DERAILLEURS", DefaultMinutes = 30, BasePriceIncVat = 38.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 108 },
        new() { Name = "Fit mech hanger", Category1 = "GEAR", Category2 = "MECHS OR DERAILLEURS", DefaultMinutes = 30, BasePriceIncVat = 38.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 109 },
        new() { Name = "Fit road brake lever (bar tape, cables, gear / brake adjust, bleed if required)", Category1 = "GEAR", Category2 = "SHIFTERS", DefaultMinutes = 45, BasePriceIncVat = 57.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 110 },
        new() { Name = "Single ring conversion", Category1 = "GEAR", Category2 = "GROUPSET", DefaultMinutes = 30, BasePriceIncVat = 38.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 111 },
        new() { Name = "Adjust brake only", Category1 = "BRAKES", Category2 = "ADJUST", DefaultMinutes = 15, BasePriceIncVat = 19.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 112 },
        new() { Name = "Fit cable to road bike pair - including fitting new bar tape", Category1 = "BRAKES", Category2 = "CABLES", DefaultMinutes = 45, BasePriceIncVat = 57.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 113 },
        new() { Name = "Fit brake pads", Category1 = "BRAKES", Category2 = "PADS AND ROTORS", DefaultMinutes = 15, BasePriceIncVat = 19.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 114 },
        new() { Name = "CONTAMINATED pads - Fit pads, clean braking surface", Category1 = "BRAKES", Category2 = "PADS AND ROTORS", DefaultMinutes = 30, BasePriceIncVat = 38.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 115 },
        new() { Name = "Face brake caliper mount", Category1 = "BRAKES", Category2 = "CALIPERS", DefaultMinutes = 30, BasePriceIncVat = 38.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 116 },
        new() { Name = "Fit brake lever", Category1 = "BRAKES", Category2 = "LEVERS", DefaultMinutes = 15, BasePriceIncVat = 19.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 117 },
        new() { Name = "Fit brake levers - pair", Category1 = "BRAKES", Category2 = "LEVERS", DefaultMinutes = 30, BasePriceIncVat = 38.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 118 },
        new() { Name = "Fit road brake lever (bar tape, cables, gear / brake adjust, bleed if required)", Category1 = "BRAKES", Category2 = "LEVERS", DefaultMinutes = 45, BasePriceIncVat = 57.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 119 },
        new() { Name = "Fit MTB hydraulic lever, hose and caliper - including bleed", Category1 = "BRAKES", Category2 = "FULL SYSTEM", DefaultMinutes = 45, BasePriceIncVat = 57.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 120 },
        new() { Name = "Bleed brake and adjust", Category1 = "BRAKES", Category2 = "BRAKE BLEED", DefaultMinutes = 30, BasePriceIncVat = 38.00m, PricingMode = ServicePricingMode.AutoRate, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 121 },
        new() { Name = "LABOUR - FOC GOODWILL", Category1 = "LABOUR", Category2 = "LABOUR", DefaultMinutes = 0, BasePriceIncVat = 0.00m, PricingMode = ServicePricingMode.FixedPrice, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 122 },
        new() { Name = "LABOUR - FOC QUOTE", Category1 = "LABOUR", Category2 = "LABOUR", DefaultMinutes = 0, BasePriceIncVat = 0.00m, PricingMode = ServicePricingMode.FixedPrice, AutoPricingTier = ServiceHourlyRateTier.Default, SortOrder = 123 },
        new() { Name = "WARRANTY - LABOUR BOSCH", Category1 = "WARRANTY", Category2 = "WARRANTY SERVICING", DefaultMinutes = 0, BasePriceIncVat = 0.00m, PricingMode = ServicePricingMode.EstimatedPrice, AutoPricingTier = ServiceHourlyRateTier.Default, EstimatedPriceIncVat = 0.00m, SortOrder = 124 },
        new() { Name = "WARRANTY - LABOUR EXTRA", Category1 = "WARRANTY", Category2 = "WARRANTY SERVICING", DefaultMinutes = 0, BasePriceIncVat = 0.00m, PricingMode = ServicePricingMode.EstimatedPrice, AutoPricingTier = ServiceHourlyRateTier.Default, EstimatedPriceIncVat = 0.00m, SortOrder = 125 },
        new() { Name = "WARRANTY - LABOUR GIANT", Category1 = "WARRANTY", Category2 = "WARRANTY SERVICING", DefaultMinutes = 0, BasePriceIncVat = 0.00m, PricingMode = ServicePricingMode.EstimatedPrice, AutoPricingTier = ServiceHourlyRateTier.Default, EstimatedPriceIncVat = 0.00m, SortOrder = 126 },
        new() { Name = "WARRANTY - LABOUR MADISON", Category1 = "WARRANTY", Category2 = "WARRANTY SERVICING", DefaultMinutes = 0, BasePriceIncVat = 0.00m, PricingMode = ServicePricingMode.EstimatedPrice, AutoPricingTier = ServiceHourlyRateTier.Default, EstimatedPriceIncVat = 0.00m, SortOrder = 127 },
        new() { Name = "WARRANTY - LABOUR MIRIDER", Category1 = "WARRANTY", Category2 = "WARRANTY SERVICING", DefaultMinutes = 0, BasePriceIncVat = 0.00m, PricingMode = ServicePricingMode.EstimatedPrice, AutoPricingTier = ServiceHourlyRateTier.Default, EstimatedPriceIncVat = 0.00m, SortOrder = 128 },
        new() { Name = "WARRANTY - LABOUR PARTS FIT", Category1 = "WARRANTY", Category2 = "WARRANTY SERVICING", DefaultMinutes = 0, BasePriceIncVat = 0.00m, PricingMode = ServicePricingMode.EstimatedPrice, AutoPricingTier = ServiceHourlyRateTier.Default, EstimatedPriceIncVat = 0.00m, SortOrder = 129 },
        new() { Name = "WARRANTY - LABOUR RALEIGH", Category1 = "WARRANTY", Category2 = "WARRANTY SERVICING", DefaultMinutes = 0, BasePriceIncVat = 0.00m, PricingMode = ServicePricingMode.EstimatedPrice, AutoPricingTier = ServiceHourlyRateTier.Default, EstimatedPriceIncVat = 0.00m, SortOrder = 130 },
        new() { Name = "WARRANTY - LABOUR SADDLEBACK", Category1 = "WARRANTY", Category2 = "WARRANTY SERVICING", DefaultMinutes = 0, BasePriceIncVat = 0.00m, PricingMode = ServicePricingMode.EstimatedPrice, AutoPricingTier = ServiceHourlyRateTier.Default, EstimatedPriceIncVat = 0.00m, SortOrder = 131 },
        new() { Name = "WARRANTY - LABOUR SILVERFISH", Category1 = "WARRANTY", Category2 = "WARRANTY SERVICING", DefaultMinutes = 0, BasePriceIncVat = 0.00m, PricingMode = ServicePricingMode.EstimatedPrice, AutoPricingTier = ServiceHourlyRateTier.Default, EstimatedPriceIncVat = 0.00m, SortOrder = 132 },
        new() { Name = "WARRANTY - LABOUR SPECIALIZED", Category1 = "WARRANTY", Category2 = "WARRANTY SERVICING", DefaultMinutes = 0, BasePriceIncVat = 0.00m, PricingMode = ServicePricingMode.EstimatedPrice, AutoPricingTier = ServiceHourlyRateTier.Default, EstimatedPriceIncVat = 0.00m, SortOrder = 133 },
        new() { Name = "WARRANTY - LABOUR TREK", Category1 = "WARRANTY", Category2 = "WARRANTY SERVICING", DefaultMinutes = 0, BasePriceIncVat = 0.00m, PricingMode = ServicePricingMode.EstimatedPrice, AutoPricingTier = ServiceHourlyRateTier.Default, EstimatedPriceIncVat = 0.00m, SortOrder = 134 },
        new() { Name = "WARRANTY - LABOUR UPGRADE", Category1 = "WARRANTY", Category2 = "WARRANTY SERVICING", DefaultMinutes = 0, BasePriceIncVat = 0.00m, PricingMode = ServicePricingMode.EstimatedPrice, AutoPricingTier = ServiceHourlyRateTier.Default, EstimatedPriceIncVat = 0.00m, SortOrder = 135 },
        new() { Name = "WARRANTY - LABOUR WHYTE", Category1 = "WARRANTY", Category2 = "WARRANTY SERVICING", DefaultMinutes = 0, BasePriceIncVat = 0.00m, PricingMode = ServicePricingMode.EstimatedPrice, AutoPricingTier = ServiceHourlyRateTier.Default, EstimatedPriceIncVat = 0.00m, SortOrder = 136 },
        new() { Name = "WARRANTY - LABOUR WINDWAVE", Category1 = "WARRANTY", Category2 = "WARRANTY SERVICING", DefaultMinutes = 0, BasePriceIncVat = 0.00m, PricingMode = ServicePricingMode.EstimatedPrice, AutoPricingTier = ServiceHourlyRateTier.Default, EstimatedPriceIncVat = 0.00m, SortOrder = 137 },
        new() { Name = "WARRANTY - LABOUR ZYRO", Category1 = "WARRANTY", Category2 = "WARRANTY SERVICING", DefaultMinutes = 0, BasePriceIncVat = 0.00m, PricingMode = ServicePricingMode.EstimatedPrice, AutoPricingTier = ServiceHourlyRateTier.Default, EstimatedPriceIncVat = 0.00m, SortOrder = 138 },
        new() { Name = "WARRANTY - Shimano Crank Inspection", Category1 = "WARRANTY", Category2 = "WARRANTY SERVICING", DefaultMinutes = 0, BasePriceIncVat = 0.00m, PricingMode = ServicePricingMode.EstimatedPrice, AutoPricingTier = ServiceHourlyRateTier.Default, EstimatedPriceIncVat = 0.00m, SortOrder = 139 },
        new() { Name = "Motor Send For Service Inc Postage", Category1 = "E-BIKE SERVICING", Category2 = "SEND EXTERNAL", DefaultMinutes = 0, BasePriceIncVat = 0.00m, PricingMode = ServicePricingMode.EstimatedPrice, AutoPricingTier = ServiceHourlyRateTier.Default, EstimatedPriceIncVat = 0.00m, SortOrder = 140 },
        new() { Name = "Carbon Repair Frame/Fork Inc Postage", Category1 = "FRAME", Category2 = "SEND EXTERNAL FOR REPAIR / SERVICE / TUNE", DefaultMinutes = 0, BasePriceIncVat = 0.00m, PricingMode = ServicePricingMode.EstimatedPrice, AutoPricingTier = ServiceHourlyRateTier.Default, EstimatedPriceIncVat = 0.00m, SortOrder = 141 },
        new() { Name = "Dropper Service Send EXTERNAL Inc Carriage", Category1 = "FRAME", Category2 = "SEND EXTERNAL FOR REPAIR / SERVICE / TUNE", DefaultMinutes = 0, BasePriceIncVat = 0.00m, PricingMode = ServicePricingMode.EstimatedPrice, AutoPricingTier = ServiceHourlyRateTier.Default, EstimatedPriceIncVat = 0.00m, SortOrder = 142 },
        new() { Name = "Fox Service Charge - Inc Carriage", Category1 = "SUSPENSION", Category2 = "SEND EXTERNAL FOR REPAIR / SERVICE / TUNE", DefaultMinutes = 0, BasePriceIncVat = 0.00m, PricingMode = ServicePricingMode.EstimatedPrice, AutoPricingTier = ServiceHourlyRateTier.Default, EstimatedPriceIncVat = 0.00m, SortOrder = 143 },
        new() { Name = "TF Tuned Service Charge - Inc Carriage", Category1 = "SUSPENSION", Category2 = "SEND EXTERNAL FOR REPAIR / SERVICE / TUNE", DefaultMinutes = 0, BasePriceIncVat = 0.00m, PricingMode = ServicePricingMode.EstimatedPrice, AutoPricingTier = ServiceHourlyRateTier.Default, EstimatedPriceIncVat = 0.00m, SortOrder = 144 },
    };

    public static List<GlobalServicePackageTemplate> GlobalServicePackageTemplates() => new()
    {
        new()
        {
            Id = 1,
            Name = "Bronze service",
            SkillLevel = "Beginner",
            Description = "Entry-level service package.",
            DefaultMinutes = 60,
            BasePriceIncVat = 79.00m,
            PricingMode = ServicePricingMode.FixedPrice,
            AutoPricingTier = ServiceHourlyRateTier.Default,
            SortOrder = 0,
            Items = new List<GlobalServicePackageItemDefinition>
            {
                new()
                {
                    SortOrder = 0,
                    ItemType = GlobalServicePackageItemType.Manual,
                    Name = "Safety check",
                    Description = "Basic visual and torque safety checks."
                },
                new()
                {
                    SortOrder = 1,
                    ItemType = GlobalServicePackageItemType.Manual,
                    Name = "Brake and gear check",
                    Description = "Check operation and alignment."
                }
            }
        },
        new()
        {
            Id = 2,
            Name = "Silver service",
            SkillLevel = "Intermediate",
            Description = "Mid-tier package with linked Bronze checklist.",
            DefaultMinutes = 75,
            BasePriceIncVat = 109.00m,
            PricingMode = ServicePricingMode.FixedPrice,
            AutoPricingTier = ServiceHourlyRateTier.Default,
            SortOrder = 1,
            Items = new List<GlobalServicePackageItemDefinition>
            {
                new()
                {
                    SortOrder = 0,
                    ItemType = GlobalServicePackageItemType.IncludePackage,
                    Name = "Include Bronze service",
                    IncludedGlobalServicePackageTemplateId = 1
                },
                new()
                {
                    SortOrder = 1,
                    ItemType = GlobalServicePackageItemType.Manual,
                    Name = "Wheel true check",
                    Description = "Inspect and adjust wheel trueness as needed."
                }
            }
        },
        new()
        {
            Id = 3,
            Name = "Gold service",
            SkillLevel = "Advanced",
            Description = "Top-tier package with linked Silver checklist.",
            DefaultMinutes = 90,
            BasePriceIncVat = 149.00m,
            PricingMode = ServicePricingMode.FixedPrice,
            AutoPricingTier = ServiceHourlyRateTier.Default,
            SortOrder = 2,
            Items = new List<GlobalServicePackageItemDefinition>
            {
                new()
                {
                    SortOrder = 0,
                    ItemType = GlobalServicePackageItemType.IncludePackage,
                    Name = "Include Silver service",
                    IncludedGlobalServicePackageTemplateId = 2
                },
                new()
                {
                    SortOrder = 1,
                    ItemType = GlobalServicePackageItemType.Manual,
                    Name = "Full bolt check",
                    Description = "Comprehensive torque and fixing check."
                }
            }
        }
    };
}


