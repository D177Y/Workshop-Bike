using Workshop.Models;
using Workshop.Services;
using Workshop.Tests.TestSupport;
using static Workshop.Tests.TestSupport.TestHarness;

Run("BuildBikeDetailsText_IncludesAllParts", () =>
{
    var bike = new CustomerBikeRow
    {
        RowId = "bike-1",
        Make = "Giant",
        Model = "Trance",
        Size = "M",
        FrameNumber = "FR-123",
        StockNumber = "ST-456"
    };

    var details = BikeDetailsService.BuildBikeDetailsText(bike);
    AssertEqual("Giant Trance, Size M, Frame FR-123, Stock ST-456", details);
});

Run("ParseBikeFromDetails_MapsKnownPattern", () =>
{
    var parsed = BikeDetailsService.ParseBikeFromDetails("Trek Domane, Size 54, Frame F-9, Stock S-1");
    if (parsed is null)
        throw new InvalidOperationException("Expected parsed bike.");

    AssertEqual("Trek", parsed.Make);
    AssertEqual("Domane", parsed.Model);
    AssertEqual("54", parsed.Size);
    AssertEqual("F-9", parsed.FrameNumber);
    AssertEqual("S-1", parsed.StockNumber);
});

Run("ParseBikeFromDetails_UsesDefaults", () =>
{
    var parsed = BikeDetailsService.ParseBikeFromDetails("");
    if (parsed is not null)
        throw new InvalidOperationException("Expected null for empty details.");

    parsed = BikeDetailsService.ParseBikeFromDetails("SingleWord");
    if (parsed is null)
        throw new InvalidOperationException("Expected parsed bike.");

    AssertEqual("SingleWord", parsed.Make);
    AssertEqual("Bike", parsed.Model);
});

Run("CustomerBikeProfileMapper_ParsesBikeDetailsIntoProfile", () =>
{
    var parsed = CustomerBikeProfileMapper.ParseBikeFromDetails("Trek Domane, Size 54, Frame F-9, Stock S-1");
    if (parsed is null)
        throw new InvalidOperationException("Expected parsed bike profile.");

    AssertEqual("Trek", parsed.Make);
    AssertEqual("Domane", parsed.Model);
    AssertEqual("54", parsed.Size);
    AssertEqual("F-9", parsed.FrameNumber);
    AssertEqual("S-1", parsed.StockNumber);
});

Run("CustomerBikeProfileMapper_Fingerprint_IsStable", () =>
{
    var fingerprint = CustomerBikeProfileMapper.GetFingerprint(new CustomerBikeProfile
    {
        Make = " Trek ",
        Model = "Domane",
        Size = "54",
        FrameNumber = "F-9",
        StockNumber = "S-1"
    });

    AssertEqual("trek|domane|54|f-9|s-1", fingerprint);
});

Run("CustomerBookingProfileService_BuildsNewProfileFromBooking", () =>
{
    var service = new CustomerBookingProfileService();
    var booking = new Booking
    {
        CustomerFirstName = "Daryl",
        CustomerLastName = "Hagan",
        CustomerPhone = "07700 900123",
        CustomerEmail = "daryl@example.com",
        BikeDetails = "Trek Domane, Size 54, Frame F-9, Stock S-1"
    };

    var profile = service.BuildProfileFromBooking(booking, Array.Empty<CustomerProfile>());
    AssertEqual("Daryl", profile.FirstName);
    AssertEqual("Hagan", profile.LastName);
    AssertEqual("daryl@example.com", profile.Email);
    if (profile.Bikes.Count != 1)
        throw new InvalidOperationException("Expected bike parsed into new profile.");
});

Run("CustomerBookingProfileService_ReusesExistingAndAvoidsDuplicateBike", () =>
{
    var service = new CustomerBookingProfileService();
    var existing = new CustomerProfile
    {
        AccountNumber = "CUST-00042",
        FirstName = "Daryl",
        LastName = "Hagan",
        Email = "daryl@example.com",
        Bikes = new List<CustomerBikeProfile>
        {
            new()
            {
                Id = "bike-1",
                Make = "Trek",
                Model = "Domane",
                Size = "54",
                FrameNumber = "F-9",
                StockNumber = "S-1"
            }
        }
    };
    var booking = new Booking
    {
        CustomerEmail = "daryl@example.com",
        BikeDetails = "Trek Domane, Size 54, Frame F-9, Stock S-1"
    };

    var profile = service.BuildProfileFromBooking(booking, new[] { existing });
    if (profile.Bikes.Count != 1)
        throw new InvalidOperationException("Duplicate bike should not be added.");
});

Run("CustomerProfileCoreService_FindByAccountEmailOrPhone", () =>
{
    var profiles = new List<CustomerProfile>
    {
        new()
        {
            AccountNumber = "CUST-00042",
            Email = "daryl@example.com",
            Phone = "07700 900123",
            PhoneNormalized = "07700900123"
        }
    };

    if (CustomerProfileCoreService.FindCustomerProfileCore(profiles, "CUST-00042", null, null) is null)
        throw new InvalidOperationException("Expected profile lookup by account.");
    if (CustomerProfileCoreService.FindCustomerProfileCore(profiles, null, "daryl@example.com", null) is null)
        throw new InvalidOperationException("Expected profile lookup by email.");
    if (CustomerProfileCoreService.FindCustomerProfileCore(profiles, null, null, "07700 900123") is null)
        throw new InvalidOperationException("Expected profile lookup by phone.");
});

Run("CustomerProfileCacheMutationService_UpsertAndSort", () =>
{
    var profiles = new List<CustomerProfile>
    {
        new() { Id = 2, AccountNumber = "CUST-00002", FirstName = "Sam", LastName = "Zulu" },
        new() { Id = 1, AccountNumber = "CUST-00001", FirstName = "Alex", LastName = "Alpha" }
    };

    profiles = CustomerProfileCacheMutationService.UpsertAndSort(profiles, new CustomerProfile
    {
        Id = 3,
        AccountNumber = "CUST-00003",
        FirstName = "Bea",
        LastName = "Bravo"
    });

    if (profiles.Count != 3 || profiles[0].LastName != "Alpha" || profiles[1].LastName != "Bravo" || profiles[2].LastName != "Zulu")
        throw new InvalidOperationException("Expected sorted profiles after add.");

    profiles = CustomerProfileCacheMutationService.UpsertAndSort(profiles, new CustomerProfile
    {
        Id = 3,
        AccountNumber = "CUST-00003",
        FirstName = "Bea",
        LastName = "Aardvark"
    });

    if (profiles.Count != 3 || profiles[0].LastName != "Aardvark")
        throw new InvalidOperationException("Expected upsert by id and re-sort.");
});

Run("CustomerValidation_RequiresCoreFields", () =>
{
    var result = CustomerValidationService.ValidateRequiredFields(
        new CustomerProfileInput(
            AccountNumber: "",
            FirstName: "",
            LastName: "Hagan",
            Phone: "0123",
            Email: "",
            County: "",
            Postcode: "",
            AddressLine1: "",
            AddressLine2: "",
            Bikes: Array.Empty<CustomerBikeInput>()),
        requireEmail: false);

    if (result.IsValid)
        throw new InvalidOperationException("Validation should fail when first name is missing.");
});

Run("CustomerValidation_EmailOptionalUnlessRequested", () =>
{
    var noEmailResult = CustomerValidationService.ValidateRequiredFields(
        new CustomerProfileInput(
            AccountNumber: "",
            FirstName: "Daryl",
            LastName: "Hagan",
            Phone: "0123",
            Email: "",
            County: "",
            Postcode: "",
            AddressLine1: "",
            AddressLine2: "",
            Bikes: Array.Empty<CustomerBikeInput>()),
        requireEmail: false);

    if (!noEmailResult.IsValid)
        throw new InvalidOperationException("Validation should allow missing email when not required.");

    var requiredEmailResult = CustomerValidationService.ValidateRequiredFields(
        new CustomerProfileInput(
            AccountNumber: "",
            FirstName: "Daryl",
            LastName: "Hagan",
            Phone: "0123",
            Email: "",
            County: "",
            Postcode: "",
            AddressLine1: "",
            AddressLine2: "",
            Bikes: Array.Empty<CustomerBikeInput>()),
        requireEmail: true);

    if (requiredEmailResult.IsValid)
        throw new InvalidOperationException("Validation should fail when email is required and missing.");
});

Run("AssessmentNotes_OnlyIncludesUserNotes", () =>
{
    var notes = AssessmentNotesService.BuildBookingNotes(new[]
    {
        "Chain worn needs replacing",
        " ",
        ""
    });

    AssertEqual("1. Chain worn needs replacing", notes);
});

Run("QuoteValidation_RequiresEmailWhenEmailing", () =>
{
    var invalid = QuoteValidationService.ValidateCanEmailQuote("");
    if (invalid.IsValid)
        throw new InvalidOperationException("Email quote validation should fail when email is empty.");

    var valid = QuoteValidationService.ValidateCanEmailQuote("person@example.com");
    if (!valid.IsValid)
        throw new InvalidOperationException("Email quote validation should pass when email is present.");
});

Run("BikeSelection_ToggleSelectAndUnselect", () =>
{
    string? selected = null;
    selected = BikeSelectionService.ToggleSelection(selected, "bike-a", isChecked: true);
    AssertEqual("bike-a", selected ?? "");

    selected = BikeSelectionService.ToggleSelection(selected, "bike-a", isChecked: false);
    AssertEqual("", selected ?? "");

    selected = BikeSelectionService.ToggleSelection("bike-a", "bike-b", isChecked: false);
    AssertEqual("bike-a", selected ?? "");
});

Run("QuoteLifecycle_ResolvesExpiredAndAccepted", () =>
{
    var expired = new CustomerQuoteRecord
    {
        Id = "q-expired",
        CreatedUtc = DateTime.UtcNow.AddDays(-40),
        Status = QuoteLifecycleStatus.Sent,
        ExpiresAtUtc = DateTime.UtcNow.AddDays(-1)
    };
    AssertEqual(QuoteLifecycleStatus.Expired, QuoteLifecycleService.ResolveStatus(expired, DateTime.UtcNow));

    var accepted = new CustomerQuoteRecord
    {
        Id = "q-accepted",
        CreatedUtc = DateTime.UtcNow.AddDays(-5),
        Status = QuoteLifecycleStatus.Sent,
        AcceptedAtUtc = DateTime.UtcNow.AddHours(-2)
    };
    AssertEqual(QuoteLifecycleStatus.Accepted, QuoteLifecycleService.ResolveStatus(accepted, DateTime.UtcNow));
});

Run("CustomerTimeline_BuildsDescendingTimeline", () =>
{
    var now = DateTime.UtcNow;
    var customer = new CustomerProfile
    {
        AccountNumber = "CUST-1",
        FirstName = "Daryl",
        LastName = "Hagan",
        Email = "daryl@example.com",
        Quotes = new List<CustomerQuoteRecord>
        {
            new()
            {
                Id = "q1",
                CreatedUtc = now.AddHours(-5),
                Status = QuoteLifecycleStatus.Sent,
                SentAtUtc = now.AddHours(-4),
                EstimatedPriceIncVat = 50,
                StoreName = "Main"
            }
        },
        Communications = new List<CustomerCommunicationRecord>
        {
            new()
            {
                SentAtUtc = now.AddHours(-1),
                Channel = "Email",
                Recipient = "daryl@example.com",
                Summary = "Manual follow-up",
                Source = "Manual"
            }
        }
    };

    var bookings = new List<Booking>
    {
        new()
        {
            Id = 1234,
            Start = now.AddHours(-3),
            Title = "Service",
            StatusName = "Scheduled",
            TotalPriceIncVat = 60
        }
    };

    var timeline = CustomerTimelineService.BuildTimeline(customer, bookings, now);
    if (timeline.Count < 3)
        throw new InvalidOperationException("Expected combined timeline entries.");

    var sorted = timeline.OrderByDescending(x => x.OccurredUtc).ToList();
    for (var i = 0; i < timeline.Count; i++)
    {
        if (timeline[i].OccurredUtc != sorted[i].OccurredUtc)
            throw new InvalidOperationException("Timeline entries are not sorted descending.");
    }
});

Run("TimetasticMapper_MapsFullDayHoliday", () =>
{
    var ok = TimetasticTimeOffMapper.TryMapToTimeOff(
        sourceId: 123,
        startDateRaw: "2026-03-02T00:00:00",
        startTypeRaw: "Morning",
        endDateRaw: "2026-03-02T00:00:00",
        endTypeRaw: "Afternoon",
        bookingUnitRaw: "Days",
        leaveTypeRaw: "Holiday",
        reasonRaw: "",
        storeId: 14,
        mechanicId: 7,
        tenantId: 25,
        includeTenantId: true,
        out var entry);

    if (!ok)
        throw new InvalidOperationException("Expected mapping to succeed.");
    AssertEqual("Timetastic", entry.Source);
    AssertEqual(TimetasticTimeOffIdentity.BuildExternalId(123), entry.ExternalId);
    if (!entry.IsAllDay)
        throw new InvalidOperationException("Expected full-day mapping.");
});

Run("TimetasticMapper_MapsHourlyHoliday", () =>
{
    var ok = TimetasticTimeOffMapper.TryMapToTimeOff(
        sourceId: 124,
        startDateRaw: "2026-03-02T09:00:00",
        startTypeRaw: "Hours",
        endDateRaw: "2026-03-02T17:30:00",
        endTypeRaw: "Hours",
        bookingUnitRaw: "Hours",
        leaveTypeRaw: "Training",
        reasonRaw: "Course",
        storeId: 14,
        mechanicId: 7,
        tenantId: 25,
        includeTenantId: true,
        out var entry);

    if (!ok)
        throw new InvalidOperationException("Expected hourly mapping to succeed.");
    if (entry.IsAllDay)
        throw new InvalidOperationException("Expected non-all-day mapping.");
    AssertEqual("Training", entry.Type);
});

Run("TimetasticTimeOffIdentity_BuildsStableExternalId", () =>
{
    AssertEqual("holiday:42", TimetasticTimeOffIdentity.BuildExternalId(42));
});

Run("TimeOffOrdering_SortsBySoonest", () =>
{
    var sorted = TimeOffOrderingService.OrderBySoonest(new[]
    {
        new MechanicTimeOff { Id = 3, Start = new DateTime(2026,3,4,9,0,0), End = new DateTime(2026,3,4,10,0,0) },
        new MechanicTimeOff { Id = 1, Start = new DateTime(2026,3,2,9,0,0), End = new DateTime(2026,3,2,10,0,0) },
        new MechanicTimeOff { Id = 2, Start = new DateTime(2026,3,2,8,0,0), End = new DateTime(2026,3,2,9,0,0) }
    });

    if (sorted[0].Id != 2 || sorted[1].Id != 1 || sorted[2].Id != 3)
        throw new InvalidOperationException("Time-off ordering is incorrect.");
});

Run("TimeOffOrdering_UsesEndThenIdAsTieBreakers", () =>
{
    var sorted = TimeOffOrderingService.OrderBySoonest(new[]
    {
        new MechanicTimeOff { Id = 5, Start = new DateTime(2026,3,2,9,0,0), End = new DateTime(2026,3,2,11,0,0) },
        new MechanicTimeOff { Id = 4, Start = new DateTime(2026,3,2,9,0,0), End = new DateTime(2026,3,2,10,0,0) },
        new MechanicTimeOff { Id = 3, Start = new DateTime(2026,3,2,9,0,0), End = new DateTime(2026,3,2,10,0,0) }
    });

    if (sorted[0].Id != 3 || sorted[1].Id != 4 || sorted[2].Id != 5)
        throw new InvalidOperationException("Time-off tie-break ordering is incorrect.");
});

Run("BookingInsertPolicy_ResetsIdToZero", () =>
{
    var booking = new Booking { Id = 999 };
    BookingInsertPolicy.NormalizeForInsert(booking);
    if (booking.Id != 0)
        throw new InvalidOperationException("Booking ID should be reset to 0 before insert.");
});

Run("TimetasticWebhookParser_ParsesCanonicalJsonPayload", () =>
{
    var parser = new TimetasticWebhookPayloadParser();
    var payload = parser.Parse("""
    {"eventId":585030,"eventType":"AbsenceRequested","recordId":37130539,"recordData":{"id":37130539,"startDate":"2026-03-02T00:00:00","startType":"Morning","endDate":"2026-03-02T00:00:00","endType":"Afternoon","userId":161562,"userName":"Daryl Hagan","status":"Pending","bookingUnit":"Days","leaveType":"Holiday","reason":""}}
    """);

    if (payload is null || payload.EventId != 585030 || payload.RecordData is null)
        throw new InvalidOperationException("Expected parser to map canonical webhook JSON.");

    if (payload.RecordData.UserId != "161562")
        throw new InvalidOperationException("Expected numeric userId to be normalized to string.");
});

Run("TimetasticWebhookParser_ParsesFormEncodedPayload", () =>
{
    var parser = new TimetasticWebhookPayloadParser();
    var formPayload = "eventId=585031&eventType=AbsenceApproved&recordId=37130540&recordData.userId=161562&recordData.userName=Daryl+Hagan&recordData.startDate=2026-03-03T00%3A00%3A00&recordData.startType=Morning&recordData.endDate=2026-03-03T00%3A00%3A00&recordData.endType=Afternoon&recordData.bookingUnit=Days&recordData.leaveType=Holiday";
    var payload = parser.Parse(formPayload);

    if (payload is null || payload.EventId != 585031 || payload.RecordData is null)
        throw new InvalidOperationException("Expected parser to map form webhook payload.");

    if (payload.RecordData.UserName != "Daryl Hagan")
        throw new InvalidOperationException("Expected URL-decoded user name.");
});

Run("TimetasticWebhookParser_ParsesArrayPayload", () =>
{
    var parser = new TimetasticWebhookPayloadParser();
    var payload = parser.Parse("""
    [{"id":585032,"type":"AbsenceApproved","recordId":37130541,"recordData":{"id":37130541,"startDate":"2026-03-04T00:00:00","startType":"Morning","endDate":"2026-03-04T00:00:00","endType":"Afternoon","userId":"161562","userName":"Daryl Hagan"}}]
    """);

    if (payload is null)
        throw new InvalidOperationException("Expected array payload parse.");

    if (payload.EventId != 585032 || payload.EventType != "AbsenceApproved")
        throw new InvalidOperationException("Expected id/type aliases to map from array payload.");
});

Run("TimetasticWebhookParser_ParsesMinimalEventEnvelope", () =>
{
    var parser = new TimetasticWebhookPayloadParser();
    var payload = parser.Parse("""
    {"eventId":585999,"eventType":"AbsenceCancelled","recordId":37139999}
    """);

    if (payload is null)
        throw new InvalidOperationException("Expected minimal envelope payload to parse.");

    if (payload.EventId != 585999 || payload.EventType != "AbsenceCancelled")
        throw new InvalidOperationException("Expected parser to map minimal event envelope.");

    if (payload.RecordData is not null)
        throw new InvalidOperationException("Expected recordData to remain null when omitted.");
});

Run("TimetasticWebhookParser_ParsesWrappedPayloadField", () =>
{
    var parser = new TimetasticWebhookPayloadParser();
    var wrapped = "payload=%7B%22eventId%22%3A585033%2C%22eventType%22%3A%22AbsenceBooked%22%2C%22recordId%22%3A37130542%2C%22recordData%22%3A%7B%22id%22%3A37130542%2C%22startDate%22%3A%222026-03-05T00%3A00%3A00%22%2C%22startType%22%3A%22Morning%22%2C%22endDate%22%3A%222026-03-05T00%3A00%3A00%22%2C%22endType%22%3A%22Afternoon%22%2C%22userId%22%3A161562%2C%22userName%22%3A%22Daryl+Hagan%22%7D%7D";
    var payload = parser.Parse(wrapped);

    if (payload is null || payload.EventId != 585033)
        throw new InvalidOperationException("Expected wrapped payload field to parse.");
});

Run("TimetasticWebhookTenantResolver_DetectsAmbiguousSecrets", () =>
{
    var resolution = TimetasticWebhookTenantResolver.ResolveBySecret("same-secret", new List<IntegrationSettings>
    {
        new() { TenantId = 26, TimetasticEnabled = true, TimetasticWebhookSecret = "same-secret" },
        new() { TenantId = 27, TimetasticEnabled = true, TimetasticWebhookSecret = "same-secret" }
    });

    if (!resolution.IsAmbiguous)
        throw new InvalidOperationException("Expected ambiguous secret resolution.");
});

Run("TimetasticWebhookTenantResolver_FindsSingleTenant", () =>
{
    var resolution = TimetasticWebhookTenantResolver.ResolveBySecret("secret-26", new List<IntegrationSettings>
    {
        new() { TenantId = 26, TimetasticEnabled = true, TimetasticWebhookSecret = "secret-26" },
        new() { TenantId = 27, TimetasticEnabled = true, TimetasticWebhookSecret = "secret-27" }
    });

    if (resolution.Settings?.TenantId != 26)
        throw new InvalidOperationException("Expected secret to route to tenant 26.");
});

Run("StoreSchedulerProjection_BuildCustomerName_UsesFirstAndLast", () =>
{
    var projection = new StoreSchedulerBookingProjectionService(new JobCatalogService());
    var booking = new Booking
    {
        CustomerFirstName = "Daryl",
        CustomerLastName = "Hagan"
    };

    var name = projection.BuildCustomerName(booking);
    AssertEqual("Daryl Hagan", name);
});

Run("StoreSchedulerProjection_BuildBookingNotes_PrefersJobCardNotes", () =>
{
    var projection = new StoreSchedulerBookingProjectionService(new JobCatalogService());
    var booking = new Booking
    {
        Notes = "Legacy notes",
        JobCard = new BookingJobCard
        {
            ServiceNotes = "Service detail",
            CustomerNotes = "Customer detail"
        }
    };

    var notes = projection.BuildBookingNotes(booking);
    AssertEqual("Service: Service detail | Customer: Customer detail", notes);
});

Run("StoreSchedulerProjection_ResolveBookingJobIds_FallsBackToJobId", () =>
{
    var catalog = new JobCatalogService
    {
        Jobs =
        {
            new JobDefinition { Id = "J1", Name = "Bronze Service", Category = "Services", DefaultMinutes = 60, BasePriceIncVat = 79m }
        }
    };
    var projection = new StoreSchedulerBookingProjectionService(catalog);
    var booking = new Booking
    {
        JobId = "J1",
        JobIds = Array.Empty<string>(),
        AddOnIds = Array.Empty<string>()
    };

    var ids = projection.ResolveBookingJobIds(booking);
    if (ids.Count != 1 || ids[0] != "J1")
        throw new InvalidOperationException("Expected fallback to JobId when JobIds is empty.");
});

Run("StoreSchedulerProjection_ResolveBookingJobIds_MapsAddOnsByName", () =>
{
    var catalog = new JobCatalogService
    {
        Jobs =
        {
            new JobDefinition { Id = "P1", Name = "Service Package", Category = "Service Packages", DefaultMinutes = 120, BasePriceIncVat = 149m },
            new JobDefinition { Id = "A1", Name = "Wheel True", Category = "Services", DefaultMinutes = 30, BasePriceIncVat = 25m }
        },
        AddOns =
        {
            new AddOnDefinition { Id = "LEGACY-ADDON", Name = "Wheel True", Category = "Legacy" }
        }
    };
    var projection = new StoreSchedulerBookingProjectionService(catalog);
    var booking = new Booking
    {
        JobIds = new[] { "P1" },
        AddOnIds = new[] { "LEGACY-ADDON" }
    };

    var ids = projection.ResolveBookingJobIds(booking);
    if (!ids.Contains("P1") || !ids.Contains("A1") || ids.Count != 2)
        throw new InvalidOperationException("Expected add-on mapping to include matching non-package job id.");
});

Run("StoreSchedulerColor_NormalizeColor_AddsHashAndTrims", () =>
{
    AssertEqual("#abc123", StoreSchedulerColorService.NormalizeColor("abc123"));
    AssertEqual("#A1B2C3", StoreSchedulerColorService.NormalizeColor("  #A1B2C3 "));
    AssertEqual("", StoreSchedulerColorService.NormalizeColor(" "));
});

Run("StoreSchedulerColor_SanitizeColor_RemovesHashAndLowercases", () =>
{
    AssertEqual("abc123", StoreSchedulerColorService.SanitizeColor("#ABC123"));
    AssertEqual("ff00ee", StoreSchedulerColorService.SanitizeColor(" ff00EE "));
    AssertEqual("", StoreSchedulerColorService.SanitizeColor(""));
});

Run("CreateBookingStepState_PreventsForwardWhenInvalid", () =>
{
    var state = new CreateBookingStepState();
    state.MoveNext(canProceed: false, lastStep: 3);

    if (state.ActiveStep != 0)
        throw new InvalidOperationException("Step should not advance when invalid.");
    if (!state.ShowValidation)
        throw new InvalidOperationException("Validation flag should be set when invalid.");
});

Run("CreateBookingStepState_AdvancesAndHandlesStepperBackNavigation", () =>
{
    var state = new CreateBookingStepState();
    state.MoveNext(canProceed: true, lastStep: 3);
    state.MoveNext(canProceed: true, lastStep: 3);

    if (state.ActiveStep != 2)
        throw new InvalidOperationException("Step should advance when valid.");

    state.SetActiveFromStepper(1);
    if (state.ActiveStep != 1)
        throw new InvalidOperationException("Stepper should allow back navigation.");

    state.SetActiveFromStepper(3);
    if (state.ActiveStep != 1)
        throw new InvalidOperationException("Stepper should ignore forward navigation.");
});

Run("TimetasticEventTypePolicy_ClassifiesWebhookEvents", () =>
{
    if (!TimetasticEventTypePolicy.IsTest(" TestEvent "))
        throw new InvalidOperationException("Expected TestEvent classification.");

    if (!TimetasticEventTypePolicy.ShouldUpsert("AbsenceApproved"))
        throw new InvalidOperationException("Expected AbsenceApproved to upsert.");

    if (!TimetasticEventTypePolicy.ShouldDelete("AbsenceCancelled"))
        throw new InvalidOperationException("Expected AbsenceCancelled to delete.");

    if (TimetasticEventTypePolicy.IsSupported("AbsenceRequested"))
        throw new InvalidOperationException("AbsenceRequested should remain unsupported.");
});

Run("TimetasticApiConfiguration_NormalizesTokenAndBaseUri", () =>
{
    AssertEqual("abc-123", TimetasticApiConfiguration.NormalizeToken("Bearer abc-123"));
    AssertEqual("abc-123", TimetasticApiConfiguration.NormalizeToken("  abc-123  "));

    var defaultUri = TimetasticApiConfiguration.NormalizeBaseUri(null);
    AssertEqual("https://app.timetastic.co.uk/api/", defaultUri?.ToString() ?? "");

    var customUri = TimetasticApiConfiguration.NormalizeBaseUri("https://example.com/root");
    AssertEqual("https://example.com/root/", customUri?.ToString() ?? "");
});

Run("WorkshopCacheMutationService_AddStore_SortsByName", () =>
{
    var stores = new List<Store>
    {
        new() { Id = 2, Name = "Bristol" }
    };

    var updated = WorkshopCacheMutationService.AddStore(stores, new Store { Id = 1, Name = "Bath" });
    if (updated.Count != 2 || updated[0].Name != "Bath" || updated[1].Name != "Bristol")
        throw new InvalidOperationException("Stores should be sorted by name after add.");
});

Run("WorkshopCacheMutationService_RemoveMechanic_RemovesAndKeepsOthers", () =>
{
    var mechanics = new List<Mechanic>
    {
        new() { Id = 1, Name = "A" },
        new() { Id = 2, Name = "B" }
    };

    var updated = WorkshopCacheMutationService.RemoveMechanic(mechanics, 1);
    if (updated.Count != 1 || updated[0].Id != 2)
        throw new InvalidOperationException("Mechanic removal should remove target id only.");
});

Run("WorkshopCacheMutationService_UpsertBooking_UpdatesExisting", () =>
{
    var bookings = new List<Booking>
    {
        new() { Id = 10, Title = "Old", StoreId = 1, MechanicId = 2 }
    };

    WorkshopCacheMutationService.UpsertBooking(bookings, new Booking
    {
        Id = 10,
        Title = "Updated",
        StoreId = 1,
        MechanicId = 2
    });

    if (bookings.Count != 1 || bookings[0].Title != "Updated")
        throw new InvalidOperationException("Booking upsert should update existing entry.");
});

Run("WorkshopCacheMutationService_UpsertAndOrderTimeOff_SortsBySoonest", () =>
{
    var entries = new List<MechanicTimeOff>
    {
        new() { Id = 2, Start = new DateTime(2026, 3, 5, 9, 0, 0), End = new DateTime(2026, 3, 5, 17, 0, 0) }
    };

    entries = WorkshopCacheMutationService.UpsertAndOrderTimeOff(entries, new MechanicTimeOff
    {
        Id = 1,
        Start = new DateTime(2026, 3, 1, 9, 0, 0),
        End = new DateTime(2026, 3, 1, 17, 0, 0)
    });

    if (entries.Count != 2 || entries[0].Id != 1)
        throw new InvalidOperationException("Time-off upsert should order by soonest.");
});

Run("BookingStatusCacheMutationService_UpsertAndRemove", () =>
{
    var statuses = new List<BookingStatus>
    {
        new() { Name = "Scheduled", ColorHex = "#0000ff" }
    };

    statuses = BookingStatusCacheMutationService.Upsert(statuses, new BookingStatus
    {
        Name = "Scheduled",
        ColorHex = "#00ff00"
    });
    if (statuses.Count != 1 || statuses[0].ColorHex != "#00ff00")
        throw new InvalidOperationException("Status upsert should update existing item.");

    statuses = BookingStatusCacheMutationService.Upsert(statuses, new BookingStatus
    {
        Name = "In Progress",
        ColorHex = "#ff9900"
    });
    if (statuses.Count != 2)
        throw new InvalidOperationException("Status upsert should add new item.");

    statuses = BookingStatusCacheMutationService.RemoveByName(statuses, "Scheduled");
    if (statuses.Count != 1 || statuses[0].Name != "In Progress")
        throw new InvalidOperationException("Status remove should remove by name.");
});

Run("CreateBookingProgressValidator_ValidatesPerStep", () =>
{
    var step0 = CreateBookingProgressValidator.CanProceed(new CreateBookingProgressState(
        ActiveStep: 0,
        HasStore: true,
        HasJobs: false,
        HasSelectedDate: false,
        HasCustomerFirstName: false,
        HasCustomerLastName: false,
        HasCustomerPhone: false,
        HasCustomerEmail: false,
        HasBikeDetails: false));
    if (step0)
        throw new InvalidOperationException("Step 0 should require store and jobs.");

    var step2 = CreateBookingProgressValidator.CanProceed(new CreateBookingProgressState(
        ActiveStep: 2,
        HasStore: true,
        HasJobs: true,
        HasSelectedDate: true,
        HasCustomerFirstName: true,
        HasCustomerLastName: true,
        HasCustomerPhone: true,
        HasCustomerEmail: true,
        HasBikeDetails: true));
    if (!step2)
        throw new InvalidOperationException("Step 2 should pass when customer fields are complete.");

    var step3 = CreateBookingProgressValidator.CanProceed(new CreateBookingProgressState(
        ActiveStep: 3,
        HasStore: true,
        HasJobs: true,
        HasSelectedDate: true,
        HasCustomerFirstName: true,
        HasCustomerLastName: true,
        HasCustomerPhone: true,
        HasCustomerEmail: false,
        HasBikeDetails: true));
    if (step3)
        throw new InvalidOperationException("Step 3 should fail when any required field is missing.");
});

Run("PlanCatalog_MechanicCaps_AreCanonical", () =>
{
    if (PlanCatalog.GetMechanicLimit(PlanTier.Starter) != 1)
        throw new InvalidOperationException("Starter mechanic limit mismatch.");
    if (PlanCatalog.GetMechanicLimit(PlanTier.Standard) != 6)
        throw new InvalidOperationException("Standard mechanic limit mismatch.");
    if (PlanCatalog.GetMechanicLimit(PlanTier.Premium) != 30)
        throw new InvalidOperationException("Premium mechanic limit mismatch.");
    if (PlanCatalog.GetMechanicLimit(PlanTier.Enterprise) != 50)
        throw new InvalidOperationException("Enterprise mechanic limit mismatch.");
});

Run("PlanCatalog_ParsesPlanKeys", () =>
{
    if (!PlanCatalog.TryParseKey("starter", out var starter) || starter != PlanTier.Starter)
        throw new InvalidOperationException("Expected starter plan key parse.");
    if (!PlanCatalog.TryParseKey("PREMIUM", out var premium) || premium != PlanTier.Premium)
        throw new InvalidOperationException("Expected premium plan key parse.");
    if (!string.Equals(PlanCatalog.ToKey(PlanTier.Enterprise), "enterprise", StringComparison.Ordinal))
        throw new InvalidOperationException("Expected enterprise key mapping.");
});

Run("StripeBillingPolicy_HasBillableAccessRules", () =>
{
    var activeTenant = new Tenant
    {
        StripeSubscriptionId = "sub_123",
        StripeSubscriptionStatus = "active"
    };
    if (!StripeBillingPolicy.HasBillableAccess(activeTenant))
        throw new InvalidOperationException("Active subscription should be billable.");

    var legacyTenant = new Tenant
    {
        StripeSubscriptionId = "sub_legacy",
        StripeSubscriptionStatus = ""
    };
    if (!StripeBillingPolicy.HasBillableAccess(legacyTenant))
        throw new InvalidOperationException("Legacy subscription without synced status should remain billable.");

    var cancelledTenant = new Tenant
    {
        StripeSubscriptionId = "sub_old",
        StripeSubscriptionStatus = "canceled"
    };
    if (StripeBillingPolicy.HasBillableAccess(cancelledTenant))
        throw new InvalidOperationException("Canceled subscription should not be billable.");
});

var smokeBaseUrl = HttpFlowSmokeTests.DefaultBaseUrl;
if (HttpFlowSmokeTests.IsReachable(smokeBaseUrl))
{
    Run("HttpFlowSmoke_PricingPaidMode_Loads", HttpFlowSmokeTests.PricingPaidMode_Loads);
    Run("HttpFlowSmoke_PricingCheckout_AnonymousRedirectsToLogin", HttpFlowSmokeTests.PricingCheckout_AnonymousRedirectsToLogin);
    Run("HttpFlowSmoke_SignupPage_LoadsWithPackageSelector", HttpFlowSmokeTests.SignupPage_LoadsWithPackageSelector);
    Run("HttpFlowSmoke_TrialAccess_AnonymousRedirectsToLogin", HttpFlowSmokeTests.TrialAccess_AnonymousRedirectsToLogin);
    Run("HttpFlowSmoke_Login_InvalidCredentials_RedirectsWithError", HttpFlowSmokeTests.Login_InvalidCredentials_RedirectsWithError);
}
else
{
    Console.WriteLine($"SKIP: HTTP flow smoke tests. Base URL unreachable: {smokeBaseUrl}");
}

return TestHarness.Complete();
