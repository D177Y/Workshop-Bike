using System.Text.Json;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Workshop.Models;

namespace Workshop.Data;

public sealed class WorkshopDbContext : IdentityDbContext<AppUser, AppRole, int>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public WorkshopDbContext(DbContextOptions<WorkshopDbContext> options) : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<UserMechanicAccess> UserMechanicAccess => Set<UserMechanicAccess>();
    public DbSet<UserStoreAccess> UserStoreAccess => Set<UserStoreAccess>();
    public DbSet<CatalogSettings> CatalogSettings => Set<CatalogSettings>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Mechanic> Mechanics => Set<Mechanic>();
    public DbSet<JobDefinition> JobDefinitions => Set<JobDefinition>();
    public DbSet<AddOnDefinition> AddOnDefinitions => Set<AddOnDefinition>();
    public DbSet<AddOnRule> AddOnRules => Set<AddOnRule>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingStatus> BookingStatuses => Set<BookingStatus>();
    public DbSet<MechanicTimeOff> MechanicTimeOffEntries => Set<MechanicTimeOff>();
    public DbSet<IntegrationSettings> IntegrationSettings => Set<IntegrationSettings>();
    public DbSet<TimetasticWebhookEvent> TimetasticWebhookEvents => Set<TimetasticWebhookEvent>();
    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();
    public DbSet<EmailRetryQueueItem> EmailRetryQueueItems => Set<EmailRetryQueueItem>();
    public DbSet<TrialExitFeedback> TrialExitFeedbackEntries => Set<TrialExitFeedback>();
    public DbSet<GlobalServiceCategory> GlobalServiceCategories => Set<GlobalServiceCategory>();
    public DbSet<GlobalServiceTemplate> GlobalServiceTemplates => Set<GlobalServiceTemplate>();
    public DbSet<GlobalServicePackageTemplate> GlobalServicePackageTemplates => Set<GlobalServicePackageTemplate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureTenant(modelBuilder);
        ConfigureCatalogSettings(modelBuilder);
        ConfigureStore(modelBuilder);
        ConfigureMechanic(modelBuilder);
        ConfigureJobDefinitions(modelBuilder);
        ConfigureAddOns(modelBuilder);
        ConfigureBookings(modelBuilder);
        ConfigureBookingStatuses(modelBuilder);
        ConfigureMechanicTimeOff(modelBuilder);
        ConfigureIntegrationSettings(modelBuilder);
        ConfigureTimetasticWebhookEvents(modelBuilder);
        ConfigureCustomerProfiles(modelBuilder);
        ConfigureEmailRetryQueue(modelBuilder);
        ConfigureTrialExitFeedback(modelBuilder);
        ConfigureGlobalServiceDefaults(modelBuilder);
        ConfigureUserMechanicAccess(modelBuilder);
        ConfigureUserStoreAccess(modelBuilder);
    }

    private static void ConfigureTenant(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<Tenant>()
            .Property(x => x.Name)
            .HasMaxLength(200);

        modelBuilder.Entity<Tenant>()
            .Property(x => x.Code)
            .HasMaxLength(50);

        modelBuilder.Entity<Tenant>()
            .Property(x => x.Plan)
            .HasConversion<string>()
            .HasMaxLength(40);

        modelBuilder.Entity<Tenant>()
            .Property(x => x.ContactEmail)
            .HasMaxLength(200);

        modelBuilder.Entity<Tenant>()
            .Property(x => x.ContactPhone)
            .HasMaxLength(50);

        modelBuilder.Entity<Tenant>()
            .HasIndex(x => x.TrialDataPurgedAtUtc);

        modelBuilder.Entity<Tenant>()
            .Property(x => x.StripeSubscriptionStatus)
            .HasMaxLength(40);

        modelBuilder.Entity<Tenant>()
            .HasIndex(x => x.StripeSubscriptionStatus);

        modelBuilder.Entity<Tenant>()
            .HasIndex(x => x.HasActivatedSubscription);

        modelBuilder.Entity<Tenant>()
            .Property(x => x.FinancialYearStartMonth)
            .HasDefaultValue(1);

        modelBuilder.Entity<Tenant>()
            .Property(x => x.FinancialYearStartDay)
            .HasDefaultValue(1);

        modelBuilder.Entity<Tenant>()
            .Property(x => x.FinancialYearEndMonth)
            .HasDefaultValue(12);

        modelBuilder.Entity<Tenant>()
            .Property(x => x.FinancialYearEndDay)
            .HasDefaultValue(31);
    }

    private static void ConfigureCatalogSettings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CatalogSettings>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<CatalogSettings>()
            .HasIndex(x => x.TenantId)
            .IsUnique();

        modelBuilder.Entity<CatalogSettings>()
            .Property(x => x.CategoryColors)
            .HasConversion(new JsonValueConverter<Dictionary<string, string>>(JsonOptions, () => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)))
            .HasColumnType("json");

        modelBuilder.Entity<CatalogSettings>()
            .Property(x => x.SkillLevels)
            .HasConversion(new JsonValueConverter<List<string>>(JsonOptions, () => new List<string>()))
            .HasColumnType("json");

        modelBuilder.Entity<CatalogSettings>()
            .Property(x => x.ServiceCategoryHierarchy)
            .HasConversion(new JsonValueConverter<Dictionary<string, List<string>>>(JsonOptions, () => new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)))
            .HasColumnType("json");

        modelBuilder.Entity<CatalogSettings>()
            .Property(x => x.ServicePackageAddOnTimeReductions)
            .HasConversion(new JsonValueConverter<Dictionary<string, int>>(JsonOptions, () => new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)))
            .HasColumnType("json");
    }

    private static void ConfigureStore(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Store>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<Store>()
            .Property(x => x.DaysOpen)
            .HasConversion(new JsonValueConverter<HashSet<DayOfWeek>>(JsonOptions, () => new HashSet<DayOfWeek>()))
            .HasColumnType("json");

        modelBuilder.Entity<Store>()
            .Property(x => x.HoursByDay)
            .HasConversion(new JsonValueConverter<Dictionary<DayOfWeek, StoreDayHours>>(JsonOptions, () => new Dictionary<DayOfWeek, StoreDayHours>()))
            .HasColumnType("json");
    }

    private static void ConfigureMechanic(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Mechanic>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<Mechanic>()
            .Property(x => x.DaysWorking)
            .HasConversion(new JsonValueConverter<HashSet<DayOfWeek>>(JsonOptions, () => new HashSet<DayOfWeek>()))
            .HasColumnType("json");

        modelBuilder.Entity<Mechanic>()
            .Property(x => x.HoursByDay)
            .HasConversion(new JsonValueConverter<Dictionary<DayOfWeek, StoreDayHours>>(JsonOptions, () => new Dictionary<DayOfWeek, StoreDayHours>()))
            .HasColumnType("json");

        modelBuilder.Entity<Mechanic>()
            .Property(x => x.CustomAllowedJobIds)
            .HasConversion(new JsonValueConverter<HashSet<string>>(JsonOptions, () => new HashSet<string>(StringComparer.OrdinalIgnoreCase)))
            .HasColumnType("json");
    }

    private static void ConfigureJobDefinitions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JobDefinition>()
            .HasKey(x => new { x.TenantId, x.Id });

        modelBuilder.Entity<JobDefinition>()
            .Property(x => x.PackageOverrides)
            .HasConversion(new JsonValueConverter<List<JobServicePackageOverride>>(JsonOptions, () => new List<JobServicePackageOverride>()))
            .HasColumnType("json");

        modelBuilder.Entity<JobDefinition>()
            .Property(x => x.PackageChecklistItems)
            .HasConversion(new JsonValueConverter<List<ServicePackageChecklistItemDefinition>>(JsonOptions, () => new List<ServicePackageChecklistItemDefinition>()))
            .HasColumnType("json");
    }

    private static void ConfigureAddOns(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AddOnDefinition>()
            .HasKey(x => new { x.TenantId, x.Id });

        modelBuilder.Entity<AddOnRule>()
            .HasKey(x => new { x.TenantId, x.JobId, x.AddOnId });
    }

    private static void ConfigureBookings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<Booking>()
            .Property(x => x.JobIds)
            .HasConversion(new JsonValueConverter<string[]>(JsonOptions, Array.Empty<string>))
            .HasColumnType("json");

        modelBuilder.Entity<Booking>()
            .Property(x => x.AddOnIds)
            .HasConversion(new JsonValueConverter<string[]>(JsonOptions, Array.Empty<string>))
            .HasColumnType("json");

        modelBuilder.Entity<Booking>()
            .Property(x => x.JobCard)
            .HasConversion(new JsonValueConverter<BookingJobCard?>(JsonOptions, () => null))
            .HasColumnType("json");
    }

    private static void ConfigureBookingStatuses(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BookingStatus>()
            .HasKey(x => new { x.TenantId, x.Name });
    }

    private static void ConfigureMechanicTimeOff(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MechanicTimeOff>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<MechanicTimeOff>()
            .HasIndex(x => new { x.TenantId, x.StoreId, x.MechanicId, x.Start });

        modelBuilder.Entity<MechanicTimeOff>()
            .HasIndex(x => new { x.TenantId, x.MechanicId, x.Start });
    }

    private static void ConfigureIntegrationSettings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IntegrationSettings>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<IntegrationSettings>()
            .HasIndex(x => x.TenantId)
            .IsUnique();

        modelBuilder.Entity<IntegrationSettings>()
            .Property(x => x.TimetasticMechanicMappings)
            .HasConversion(new JsonValueConverter<List<TimetasticMechanicMapping>>(JsonOptions, () => new List<TimetasticMechanicMapping>()))
            .HasColumnType("json");
    }

    private static void ConfigureTimetasticWebhookEvents(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TimetasticWebhookEvent>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<TimetasticWebhookEvent>()
            .HasIndex(x => new { x.TenantId, x.EventId })
            .IsUnique();

        modelBuilder.Entity<TimetasticWebhookEvent>()
            .HasIndex(x => new { x.TenantId, x.ReceivedUtc });

        modelBuilder.Entity<TimetasticWebhookEvent>()
            .Property(x => x.EventType)
            .HasMaxLength(80);

        modelBuilder.Entity<TimetasticWebhookEvent>()
            .Property(x => x.Outcome)
            .HasMaxLength(240);
    }

    private static void ConfigureCustomerProfiles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomerProfile>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<CustomerProfile>()
            .HasIndex(x => new { x.TenantId, x.AccountNumber })
            .IsUnique();

        modelBuilder.Entity<CustomerProfile>()
            .HasIndex(x => new { x.TenantId, x.Email });

        modelBuilder.Entity<CustomerProfile>()
            .HasIndex(x => new { x.TenantId, x.PhoneNormalized });

        modelBuilder.Entity<CustomerProfile>()
            .Property(x => x.AccountNumber)
            .HasMaxLength(40);

        modelBuilder.Entity<CustomerProfile>()
            .Property(x => x.PhoneNormalized)
            .HasMaxLength(32);

        modelBuilder.Entity<CustomerProfile>()
            .Property(x => x.Email)
            .HasMaxLength(255);

        modelBuilder.Entity<CustomerProfile>()
            .Property(x => x.Bikes)
            .HasConversion(new JsonValueConverter<List<CustomerBikeProfile>>(JsonOptions, () => new List<CustomerBikeProfile>()))
            .HasColumnType("json");

        modelBuilder.Entity<CustomerProfile>()
            .Property(x => x.Quotes)
            .HasConversion(new JsonValueConverter<List<CustomerQuoteRecord>>(JsonOptions, () => new List<CustomerQuoteRecord>()))
            .HasColumnType("json");

        modelBuilder.Entity<CustomerProfile>()
            .Property(x => x.Communications)
            .HasConversion(new JsonValueConverter<List<CustomerCommunicationRecord>>(JsonOptions, () => new List<CustomerCommunicationRecord>()))
            .HasColumnType("json");
    }

    private static void ConfigureEmailRetryQueue(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmailRetryQueueItem>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<EmailRetryQueueItem>()
            .HasIndex(x => new { x.Status, x.NextAttemptUtc });

        modelBuilder.Entity<EmailRetryQueueItem>()
            .HasIndex(x => new { x.TenantId, x.AccountNumber });

        modelBuilder.Entity<EmailRetryQueueItem>()
            .Property(x => x.AccountNumber)
            .HasMaxLength(40);

        modelBuilder.Entity<EmailRetryQueueItem>()
            .Property(x => x.Recipient)
            .HasMaxLength(320);

        modelBuilder.Entity<EmailRetryQueueItem>()
            .Property(x => x.Subject)
            .HasMaxLength(500);

        modelBuilder.Entity<EmailRetryQueueItem>()
            .Property(x => x.Source)
            .HasMaxLength(120);

        modelBuilder.Entity<EmailRetryQueueItem>()
            .Property(x => x.Status)
            .HasMaxLength(40);
    }

    private static void ConfigureTrialExitFeedback(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TrialExitFeedback>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<TrialExitFeedback>()
            .HasIndex(x => new { x.TenantId, x.SubmittedAtUtc });

        modelBuilder.Entity<TrialExitFeedback>()
            .Property(x => x.Disliked)
            .HasMaxLength(4000);

        modelBuilder.Entity<TrialExitFeedback>()
            .Property(x => x.Improvements)
            .HasMaxLength(4000);

        modelBuilder.Entity<TrialExitFeedback>()
            .Property(x => x.NoSignupReason)
            .HasMaxLength(4000);
    }

    private static void ConfigureGlobalServiceDefaults(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GlobalServiceCategory>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<GlobalServiceCategory>()
            .Property(x => x.Category1)
            .HasMaxLength(120);

        modelBuilder.Entity<GlobalServiceCategory>()
            .Property(x => x.Category2)
            .HasMaxLength(120);

        modelBuilder.Entity<GlobalServiceCategory>()
            .Property(x => x.ColorHex)
            .HasMaxLength(20);

        modelBuilder.Entity<GlobalServiceCategory>()
            .HasIndex(x => new { x.Category1, x.Category2 })
            .IsUnique();

        modelBuilder.Entity<GlobalServiceTemplate>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<GlobalServiceTemplate>()
            .Property(x => x.Name)
            .HasMaxLength(200);

        modelBuilder.Entity<GlobalServiceTemplate>()
            .Property(x => x.PartNumber)
            .HasMaxLength(120);

        modelBuilder.Entity<GlobalServiceTemplate>()
            .Property(x => x.Category1)
            .HasMaxLength(120);

        modelBuilder.Entity<GlobalServiceTemplate>()
            .Property(x => x.Category2)
            .HasMaxLength(120);

        modelBuilder.Entity<GlobalServiceTemplate>()
            .Property(x => x.SkillLevel)
            .HasMaxLength(80);

        modelBuilder.Entity<GlobalServiceTemplate>()
            .Property(x => x.PackageOverrides)
            .HasConversion(new JsonValueConverter<List<JobServicePackageOverride>>(JsonOptions, () => new List<JobServicePackageOverride>()))
            .HasColumnType("json");

        modelBuilder.Entity<GlobalServiceTemplate>()
            .HasIndex(x => new { x.Name, x.Category1, x.Category2 });

        modelBuilder.Entity<GlobalServicePackageTemplate>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<GlobalServicePackageTemplate>()
            .Property(x => x.Name)
            .HasMaxLength(200);

        modelBuilder.Entity<GlobalServicePackageTemplate>()
            .Property(x => x.SkillLevel)
            .HasMaxLength(80);

        modelBuilder.Entity<GlobalServicePackageTemplate>()
            .Property(x => x.Description)
            .HasMaxLength(2000);

        modelBuilder.Entity<GlobalServicePackageTemplate>()
            .Property(x => x.Items)
            .HasConversion(new JsonValueConverter<List<GlobalServicePackageItemDefinition>>(JsonOptions, () => new List<GlobalServicePackageItemDefinition>()))
            .HasColumnType("json");

        modelBuilder.Entity<GlobalServicePackageTemplate>()
            .HasIndex(x => x.Name)
            .IsUnique();
    }

    private static void ConfigureUserMechanicAccess(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserMechanicAccess>()
            .HasKey(x => new { x.UserId, x.MechanicId });
    }

    private static void ConfigureUserStoreAccess(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserStoreAccess>()
            .HasKey(x => new { x.UserId, x.StoreId });
    }
}

public sealed class JsonValueConverter<TValue> : ValueConverter<TValue, string>
{
    private readonly Func<TValue> _defaultFactory;

    public JsonValueConverter(JsonSerializerOptions options, Func<TValue> defaultFactory)
        : base(
            value => JsonSerializer.Serialize(value, options),
            value => JsonSerializer.Deserialize<TValue>(value, options) ?? defaultFactory())
    {
        _defaultFactory = defaultFactory;
    }
}
