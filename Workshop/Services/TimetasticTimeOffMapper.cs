using System.Globalization;
using Workshop.Models;

namespace Workshop.Services;

public static class TimetasticTimeOffMapper
{
    public static bool TryMapToTimeOff(
        int sourceId,
        string? startDateRaw,
        string? startTypeRaw,
        string? endDateRaw,
        string? endTypeRaw,
        string? bookingUnitRaw,
        string? leaveTypeRaw,
        string? reasonRaw,
        int storeId,
        int mechanicId,
        int tenantId,
        bool includeTenantId,
        out MechanicTimeOff entry)
    {
        entry = new MechanicTimeOff();
        if (sourceId <= 0)
            return false;

        if (string.IsNullOrWhiteSpace(startDateRaw) || string.IsNullOrWhiteSpace(endDateRaw))
            return false;

        if (!TryParseDate(startDateRaw, out var startDate) || !TryParseDate(endDateRaw, out var endDate))
            return false;

        var startType = (startTypeRaw ?? "").Trim();
        var endType = (endTypeRaw ?? "").Trim();
        var bookingUnit = (bookingUnitRaw ?? "").Trim();

        var isHourly = bookingUnit.Equals("Hours", StringComparison.OrdinalIgnoreCase)
                       || startType.Equals("Hours", StringComparison.OrdinalIgnoreCase)
                       || endType.Equals("Hours", StringComparison.OrdinalIgnoreCase)
                       || startDate.TimeOfDay != TimeSpan.Zero
                       || endDate.TimeOfDay != TimeSpan.Zero;

        DateTime start;
        DateTime end;
        var isAllDay = false;

        if (isHourly)
        {
            start = startDate;
            end = endDate;
        }
        else
        {
            var fullDay = startType.Equals("Morning", StringComparison.OrdinalIgnoreCase)
                          && endType.Equals("Afternoon", StringComparison.OrdinalIgnoreCase);

            if (fullDay)
            {
                isAllDay = true;
                start = startDate.Date;
                end = endDate.Date.AddDays(1).AddTicks(-1);
            }
            else
            {
                start = startDate.Date.Add(
                    startType.Equals("Afternoon", StringComparison.OrdinalIgnoreCase)
                        ? TimeSpan.FromHours(12)
                        : TimeSpan.Zero);

                end = endDate.Date.Add(
                    endType.Equals("Morning", StringComparison.OrdinalIgnoreCase)
                        ? TimeSpan.FromHours(12)
                        : TimeSpan.FromDays(1)).AddTicks(-1);
            }
        }

        if (end <= start)
            end = start.AddHours(1);

        entry = new MechanicTimeOff
        {
            TenantId = includeTenantId ? tenantId : 0,
            StoreId = storeId,
            MechanicId = mechanicId,
            Type = string.IsNullOrWhiteSpace(leaveTypeRaw) ? "Holiday" : leaveTypeRaw.Trim(),
            Start = start,
            End = end,
            IsAllDay = isAllDay,
            Notes = (reasonRaw ?? "").Trim(),
            Source = "Timetastic",
            ExternalId = TimetasticTimeOffIdentity.BuildExternalId(sourceId),
            LastSyncedUtc = DateTime.UtcNow
        };

        return true;
    }

    private static bool TryParseDate(string value, out DateTime parsed)
        => DateTime.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces,
            out parsed);
}
