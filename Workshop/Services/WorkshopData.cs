using Workshop.Data;
using Workshop.Models;

namespace Workshop.Services;

public sealed class WorkshopData
{
    private readonly DatabaseInitializer _initializer;
    private readonly TenantContext _tenantContext;
    private readonly WorkshopReadService _reads;
    private readonly StoreCommandService _storeCommands;
    private readonly MechanicCommandService _mechanicCommands;
    private readonly BookingCommandService _bookingCommands;
    private readonly BookingStatusCommandService _bookingStatusCommands;
    private readonly TimeOffCommandService _timeOffCommands;
    private readonly CustomerProfileWriteService _customerProfileWrites;
    private readonly CustomerBookingProfileService _customerBookingProfiles;
    private bool _loaded;
    private int? _loadedTenantId;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    public WorkshopData(
        DatabaseInitializer initializer,
        TenantContext tenantContext,
        WorkshopReadService reads,
        StoreCommandService storeCommands,
        MechanicCommandService mechanicCommands,
        BookingCommandService bookingCommands,
        BookingStatusCommandService bookingStatusCommands,
        TimeOffCommandService timeOffCommands,
        CustomerProfileWriteService customerProfileWrites,
        CustomerBookingProfileService customerBookingProfiles)
    {
        _initializer = initializer;
        _tenantContext = tenantContext;
        _reads = reads;
        _storeCommands = storeCommands;
        _mechanicCommands = mechanicCommands;
        _bookingCommands = bookingCommands;
        _bookingStatusCommands = bookingStatusCommands;
        _timeOffCommands = timeOffCommands;
        _customerProfileWrites = customerProfileWrites;
        _customerBookingProfiles = customerBookingProfiles;
    }

    public List<Store> Stores { get; private set; } = new();
    public List<Mechanic> Mechanics { get; private set; } = new();
    public List<Booking> Bookings { get; private set; } = new();
    public List<BookingStatus> Statuses { get; private set; } = new();
    public List<MechanicTimeOff> MechanicTimeOffEntries { get; private set; } = new();
    public List<CustomerProfile> CustomerProfiles { get; private set; } = new();

    public async Task EnsureInitializedAsync()
    {
        var tenantId = _tenantContext.TenantId;
        if (_loaded && _loadedTenantId == tenantId) return;

        await _loadLock.WaitAsync();
        try
        {
            tenantId = _tenantContext.TenantId;
            if (_loaded && _loadedTenantId == tenantId) return;
            await _initializer.EnsureInitializedAsync();
            await ReloadAsync();
            _loaded = true;
            _loadedTenantId = tenantId;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public async Task ReloadAsync()
    {
        var tenantId = _tenantContext.TenantId;
        var snapshot = await _reads.LoadSnapshotAsync(tenantId);
        Stores = snapshot.Stores;
        Mechanics = snapshot.Mechanics;
        Bookings = snapshot.Bookings;
        Statuses = snapshot.Statuses;
        MechanicTimeOffEntries = snapshot.MechanicTimeOffEntries;
        CustomerProfiles = snapshot.CustomerProfiles;
    }

    public async Task SaveStoreAsync(Store store)
    {
        await EnsureInitializedAsync();
        var created = await _storeCommands.SaveAsync(_tenantContext.TenantId, store);
        if (created)
            Stores = WorkshopCacheMutationService.AddStore(Stores, store);
    }

    public async Task DeleteStoreAsync(int storeId)
    {
        await EnsureInitializedAsync();
        var deleted = await _storeCommands.DeleteAsync(_tenantContext.TenantId, storeId);
        if (!deleted)
            return;

        WorkshopCacheMutationService.RemoveBookingsByStore(Bookings, storeId);
        WorkshopCacheMutationService.RemoveTimeOffByStore(MechanicTimeOffEntries, storeId);
        Stores = WorkshopCacheMutationService.RemoveStore(Stores, storeId);
    }

    public async Task SaveMechanicAsync(Mechanic mechanic)
    {
        await EnsureInitializedAsync();
        var created = await _mechanicCommands.SaveAsync(_tenantContext.TenantId, mechanic);
        if (created)
            Mechanics = WorkshopCacheMutationService.AddMechanic(Mechanics, mechanic);
    }

    public async Task DeleteMechanicAsync(int mechanicId)
    {
        await EnsureInitializedAsync();
        var deleted = await _mechanicCommands.DeleteAsync(_tenantContext.TenantId, mechanicId);
        if (!deleted)
            return;

        WorkshopCacheMutationService.RemoveBookingsByMechanic(Bookings, mechanicId);
        WorkshopCacheMutationService.RemoveTimeOffByMechanic(MechanicTimeOffEntries, mechanicId);
        Mechanics = WorkshopCacheMutationService.RemoveMechanic(Mechanics, mechanicId);
    }

    public async Task AddBookingAsync(Booking booking)
    {
        await EnsureInitializedAsync();
        await _bookingCommands.AddAsync(booking);
        Bookings.Add(booking);

        await UpsertCustomerFromBookingAsync(booking);
    }

    public Task UpdateBookingAsync(Booking booking) => UpdateBookingAsync(booking, syncCustomer: true);

    private async Task UpdateBookingAsync(Booking booking, bool syncCustomer)
    {
        await EnsureInitializedAsync();
        await _bookingCommands.UpdateAsync(booking);

        WorkshopCacheMutationService.UpsertBooking(Bookings, booking);

        if (syncCustomer)
            await UpsertCustomerFromBookingAsync(booking);
    }

    public async Task DeleteBookingAsync(int bookingId)
    {
        await EnsureInitializedAsync();
        var deleted = await _bookingCommands.DeleteAsync(bookingId);
        if (!deleted)
            return;

        WorkshopCacheMutationService.RemoveBookingsById(Bookings, bookingId);
    }

    public async Task SaveMechanicTimeOffAsync(MechanicTimeOff entry)
    {
        await EnsureInitializedAsync();
        var normalized = await _timeOffCommands.SaveAsync(entry);
        MechanicTimeOffEntries = WorkshopCacheMutationService.UpsertAndOrderTimeOff(MechanicTimeOffEntries, normalized);
    }

    public async Task DeleteMechanicTimeOffAsync(int id)
    {
        await EnsureInitializedAsync();
        var deleted = await _timeOffCommands.DeleteAsync(id);
        if (!deleted)
            return;

        WorkshopCacheMutationService.RemoveTimeOffById(MechanicTimeOffEntries, id);
    }

    public async Task SaveStatusAsync(BookingStatus status)
    {
        await EnsureInitializedAsync();
        await _bookingStatusCommands.SaveAsync(_tenantContext.TenantId, status);
        Statuses = BookingStatusCacheMutationService.Upsert(Statuses, status);
    }

    public async Task DeleteStatusAsync(string name)
    {
        await EnsureInitializedAsync();
        var tenantId = _tenantContext.TenantId;
        var deleted = await _bookingStatusCommands.DeleteAsync(tenantId, name);
        if (!deleted)
            return;
        Statuses = BookingStatusCacheMutationService.RemoveByName(Statuses, name);
    }

    public async Task<CustomerProfile?> FindCustomerProfileByAccountAsync(string accountNumber)
    {
        await EnsureInitializedAsync();
        var account = (accountNumber ?? "").Trim();
        if (string.IsNullOrWhiteSpace(account))
            return null;

        var existing = CustomerProfiles.FirstOrDefault(c =>
            c.AccountNumber.Equals(account, StringComparison.OrdinalIgnoreCase));
        return existing is null ? null : CustomerProfileCoreService.CloneCustomerProfile(existing);
    }

    public async Task<CustomerProfile?> FindCustomerProfileAsync(string? accountNumber, string? email, string? phone)
    {
        await EnsureInitializedAsync();
        var existing = CustomerProfileCoreService.FindCustomerProfileCore(CustomerProfiles, accountNumber, email, phone);
        return existing is null ? null : CustomerProfileCoreService.CloneCustomerProfile(existing);
    }

    public async Task<CustomerProfile> SaveCustomerProfileAsync(CustomerProfile profile)
    {
        await EnsureInitializedAsync();
        var saved = await _customerProfileWrites.SaveAsync(_tenantContext.TenantId, profile, CustomerProfiles);
        UpsertCustomerInMemory(saved);
        return CustomerProfileCoreService.CloneCustomerProfile(saved);
    }

    public async Task<CustomerProfile?> AppendQuoteAsync(string accountNumber, CustomerQuoteRecord quote)
    {
        await EnsureInitializedAsync();
        var saved = await _customerProfileWrites.AppendQuoteAsync(
            _tenantContext.TenantId,
            accountNumber,
            quote,
            CustomerProfiles);
        if (saved is null)
            return null;

        UpsertCustomerInMemory(saved);
        return CustomerProfileCoreService.CloneCustomerProfile(saved);
    }

    public async Task<CustomerProfile?> UpdateQuoteStatusAsync(string accountNumber, string quoteId, string status, string? statusDetail = null)
    {
        await EnsureInitializedAsync();
        var saved = await _customerProfileWrites.UpdateQuoteStatusAsync(
            _tenantContext.TenantId,
            accountNumber,
            quoteId,
            status,
            statusDetail,
            CustomerProfiles);
        if (saved is null)
            return null;

        UpsertCustomerInMemory(saved);
        return CustomerProfileCoreService.CloneCustomerProfile(saved);
    }

    public async Task<CustomerProfile?> AppendCommunicationAsync(string accountNumber, CustomerCommunicationRecord communication)
    {
        await EnsureInitializedAsync();
        var saved = await _customerProfileWrites.AppendCommunicationAsync(
            _tenantContext.TenantId,
            accountNumber,
            communication,
            CustomerProfiles);
        if (saved is null)
            return null;

        UpsertCustomerInMemory(saved);
        return CustomerProfileCoreService.CloneCustomerProfile(saved);
    }

    public async Task<CustomerProfile> UpsertCustomerFromBookingAsync(Booking booking)
    {
        await EnsureInitializedAsync();

        var profile = _customerBookingProfiles.BuildProfileFromBooking(booking, CustomerProfiles);

        var saved = await SaveCustomerProfileAsync(profile);

        if (!string.Equals(booking.CustomerAccountNumber, saved.AccountNumber, StringComparison.OrdinalIgnoreCase))
        {
            booking.CustomerAccountNumber = saved.AccountNumber;
            await UpdateBookingAsync(booking, syncCustomer: false);
        }

        return saved;
    }

    private void UpsertCustomerInMemory(CustomerProfile profile)
    {
        CustomerProfiles = CustomerProfileCacheMutationService.UpsertAndSort(CustomerProfiles, profile);
    }

}
