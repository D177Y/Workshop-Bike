using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Workshop.Models;
using Workshop.Services;

namespace Workshop.Components.Pages;

public partial class StoreScheduler
{
    private string NewStatusName = "";
    private string NewStatusColor = "#3b82f6";

    private bool IsStatusDialogVisible;
    private string StatusEditorMessage = "";
    private List<StatusEditorItem> StatusEditorItems = new();
    private DotNetObjectReference<StoreScheduler>? StatusDotNetRef;
    private ElementReference StatusListRef;
    private bool ShouldInitStatusDnd;

    private sealed class StatusEditorItem
    {
        public StatusEditorItem(string name, string colorHex)
        {
            OriginalName = name;
            Name = name;
            ColorHex = colorHex;
        }

        public string OriginalName { get; }
        public string Name { get; set; }
        public string ColorHex { get; set; }
    }

    private async Task AddStatus()
    {
        var name = (NewStatusName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
            return;

        if (!StatusEditorItems.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            StatusEditorItems.Add(new StatusEditorItem(name, StoreSchedulerColorService.NormalizeColor(NewStatusColor)));

        NewStatusName = "";
        await SaveStatusEdits(closeDialog: false);
    }

    private async Task OpenStatusEditor()
    {
        StatusEditorItems = Data.Statuses
            .Select(s => new StatusEditorItem(s.Name, StoreSchedulerColorService.NormalizeColor(s.ColorHex)))
            .ToList();
        StatusEditorMessage = "";
        IsStatusDialogVisible = true;
        ShouldInitStatusDnd = true;
    }

    private void CloseStatusEditor()
    {
        IsStatusDialogVisible = false;
        StatusEditorMessage = "";
    }

    private async Task RemoveStatusEditorItem(int index)
    {
        if (index < 0 || index >= StatusEditorItems.Count)
            return;

        StatusEditorItems.RemoveAt(index);
        StatusEditorMessage = "";
        await SaveStatusEdits(closeDialog: false);
    }

    private async Task SaveStatusEdits(bool closeDialog = true)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in StatusEditorItems)
        {
            item.Name = (item.Name ?? "").Trim();
            item.ColorHex = StoreSchedulerColorService.NormalizeColor(item.ColorHex);

            if (string.IsNullOrWhiteSpace(item.Name))
            {
                StatusEditorMessage = "Status names cannot be empty.";
                return;
            }

            if (!names.Add(item.Name))
            {
                StatusEditorMessage = "Status names must be unique.";
                return;
            }
        }

        var renameMap = StatusEditorItems
            .Where(i => !i.OriginalName.Equals(i.Name, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(i => i.OriginalName, i => i.Name, StringComparer.OrdinalIgnoreCase);

        var existingNames = Data.Statuses.Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var newNames = StatusEditorItems.Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var removed in existingNames.Except(newNames, StringComparer.OrdinalIgnoreCase))
        {
            await Data.DeleteStatusAsync(removed);
        }

        foreach (var item in StatusEditorItems)
        {
            await Data.SaveStatusAsync(new BookingStatus
            {
                Name = item.Name,
                ColorHex = item.ColorHex
            });
        }

        foreach (var booking in Data.Bookings)
        {
            if (renameMap.TryGetValue(booking.StatusName, out var newName))
                booking.StatusName = newName;

            if (!Data.Statuses.Any(s => s.Name.Equals(booking.StatusName, StringComparison.OrdinalIgnoreCase)))
                booking.StatusName = "Scheduled";

            await Data.UpdateBookingAsync(booking);
        }

        ReloadAppointmentsFromBookings();
        _ = ScheduleRef?.RefreshEventsAsync();

        StatusEditorItems = StatusEditorItems
            .Select(i => new StatusEditorItem(i.Name, i.ColorHex))
            .ToList();
        if (closeDialog)
            IsStatusDialogVisible = false;
        StatusEditorMessage = "";
    }

    private async Task EnsureStatusDndAsync()
    {
        try
        {
            StatusDotNetRef ??= DotNetObjectReference.Create(this);
            await JS.InvokeVoidAsync("workshopStatusDnd.init", StatusListRef, StatusDotNetRef);
        }
        catch (JSException ex)
        {
            Logger.LogWarning(ex, "Status drag-and-drop initialization failed.");
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (ShouldInitStatusDnd)
        {
            ShouldInitStatusDnd = false;
            await EnsureStatusDndAsync();
        }
    }

    [JSInvokable]
    public async Task ReorderStatus(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || toIndex < 0 || fromIndex >= StatusEditorItems.Count || toIndex >= StatusEditorItems.Count)
            return;

        if (fromIndex == toIndex)
            return;

        var item = StatusEditorItems[fromIndex];
        StatusEditorItems.RemoveAt(fromIndex);
        if (toIndex > fromIndex) toIndex--;
        StatusEditorItems.Insert(toIndex, item);
        StateHasChanged();
        await SaveStatusEdits(closeDialog: false);
    }
}
