using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Workshop.Models;

namespace Workshop.Services;

public sealed class TimetasticSyncService
{
    private static readonly StringComparer TextComparer = StringComparer.OrdinalIgnoreCase;
    private readonly HttpClient _httpClient;
    private readonly WorkshopData _data;
    private readonly IntegrationSettingsService _integrationSettings;
    private readonly ILogger<TimetasticSyncService> _logger;

    public TimetasticSyncService(
        HttpClient httpClient,
        WorkshopData data,
        IntegrationSettingsService integrationSettings,
        ILogger<TimetasticSyncService> logger)
    {
        _httpClient = httpClient;
        _data = data;
        _integrationSettings = integrationSettings;
        _logger = logger;
    }

    public async Task<TimetasticSyncResult> SyncTimeOffAsync(
        IReadOnlyCollection<int>? mechanicScope = null,
        DateTime? rangeStart = null,
        DateTime? rangeEnd = null,
        CancellationToken cancellationToken = default)
    {
        await _data.EnsureInitializedAsync();
        var settings = await _integrationSettings.GetAsync();

        var token = TimetasticApiConfiguration.NormalizeToken(settings.TimetasticApiToken);
        if (string.IsNullOrWhiteSpace(token))
            return TimetasticSyncResult.Failed("Timetastic API token is required.");

        var baseUri = TimetasticApiConfiguration.NormalizeBaseUri(settings.TimetasticApiBaseUrl);
        if (baseUri is null)
            return TimetasticSyncResult.Failed("Timetastic API base URL is invalid.");

        var localMechanics = _data.Mechanics
            .Where(m => mechanicScope is null || mechanicScope.Count == 0 || mechanicScope.Contains(m.Id))
            .OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (localMechanics.Count == 0)
            return TimetasticSyncResult.Failed("No mechanics in scope for sync.");

        List<TimetasticUserDto> users;
        try
        {
            users = await GetUsersAsync(baseUri, token, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Timetastic users fetch failed.");
            return TimetasticSyncResult.Failed($"Could not fetch Timetastic users. {ex.Message}");
        }

        if (users.Count == 0)
            return TimetasticSyncResult.Failed("No users returned from Timetastic API.");

        var resolvedMappings = ResolveMappings(settings, localMechanics, users);
        if (resolvedMappings.Resolved.Count == 0)
        {
            var missing = resolvedMappings.UnresolvedMechanicNames.Count > 0
                ? $" Unresolved mechanics: {string.Join(", ", resolvedMappings.UnresolvedMechanicNames)}."
                : "";
            return TimetasticSyncResult.Failed($"No mechanic mappings could be resolved.{missing}");
        }

        var syncStart = (rangeStart ?? DateTime.Today.AddDays(-30)).Date;
        var syncEnd = (rangeEnd ?? DateTime.Today.AddDays(365)).Date;
        if (syncEnd < syncStart)
            (syncStart, syncEnd) = (syncEnd, syncStart);

        List<TimetasticHolidayDto> holidays;
        try
        {
            holidays = await GetHolidaysAsync(baseUri, token, resolvedMappings.Resolved, syncStart, syncEnd, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Timetastic holidays fetch failed.");
            return TimetasticSyncResult.Failed($"Could not fetch Timetastic holidays. {ex.Message}");
        }

        var resolvedByUserId = resolvedMappings.Resolved
            .GroupBy(m => m.TimetasticUserId)
            .ToDictionary(g => g.Key, g => g.First());

        var relevantHolidays = holidays
            .Where(h => h.UserId.HasValue && resolvedByUserId.ContainsKey(h.UserId.Value))
            .ToList();

        var managedMechanicIds = resolvedMappings.Resolved
            .Select(r => r.MechanicId)
            .ToHashSet();

        var rangeEndInclusive = syncEnd.AddDays(1).AddTicks(-1);
        var existingTimetasticEntries = _data.MechanicTimeOffEntries
            .Where(e => managedMechanicIds.Contains(e.MechanicId))
            .Where(e => (e.Source ?? "").Equals("Timetastic", StringComparison.OrdinalIgnoreCase))
            .Where(e => e.Start <= rangeEndInclusive && e.End >= syncStart)
            .ToList();

        var existingByKey = existingTimetasticEntries.ToDictionary(
            e => $"{e.MechanicId}:{(e.ExternalId ?? "").Trim()}",
            e => e,
            StringComparer.OrdinalIgnoreCase);

        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var created = 0;
        var updated = 0;
        foreach (var holiday in relevantHolidays)
        {
            if (!holiday.UserId.HasValue)
                continue;

            if (!resolvedByUserId.TryGetValue(holiday.UserId.Value, out var mapping))
                continue;

            if (!TryMapHolidayToTimeOff(holiday, mapping, out var mapped))
                continue;

            var key = $"{mapping.MechanicId}:{mapped.ExternalId}";
            seenKeys.Add(key);

            if (existingByKey.TryGetValue(key, out var existing))
            {
                mapped.Id = existing.Id;
                updated++;
            }
            else
            {
                created++;
            }

            await _data.SaveMechanicTimeOffAsync(mapped);
        }

        var deleted = 0;
        foreach (var stale in existingTimetasticEntries)
        {
            var key = $"{stale.MechanicId}:{(stale.ExternalId ?? "").Trim()}";
            if (seenKeys.Contains(key))
                continue;

            await _data.DeleteMechanicTimeOffAsync(stale.Id);
            deleted++;
        }

        var result = new TimetasticSyncResult
        {
            Success = true,
            Message = "Timetastic sync complete.",
            CreatedCount = created,
            UpdatedCount = updated,
            DeletedCount = deleted,
            ConsideredHolidayCount = relevantHolidays.Count,
            ResolvedMappingCount = resolvedMappings.Resolved.Count,
            UnresolvedMechanics = resolvedMappings.UnresolvedMechanicNames
        };

        settings.TimetasticLastSyncUtc = DateTime.UtcNow;
        await _integrationSettings.SaveAsync(settings);

        return result;
    }

    private async Task<List<TimetasticUserDto>> GetUsersAsync(Uri baseUri, string token, CancellationToken cancellationToken)
    {
        using var request = BuildRequest(new Uri(baseUri, "users"), token);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Timetastic users request failed ({(int)response.StatusCode}): {body}");

        var users = JsonSerializer.Deserialize<List<TimetasticUserDto>>(body, JsonOptions) ?? new List<TimetasticUserDto>();
        return users
            .Where(u => u.Id > 0)
            .ToList();
    }

    private async Task<List<TimetasticHolidayDto>> GetHolidaysAsync(
        Uri baseUri,
        string token,
        IReadOnlyCollection<ResolvedTimetasticMapping> mappings,
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken)
    {
        var userIds = mappings
            .Select(m => m.TimetasticUserId)
            .Distinct()
            .OrderBy(id => id)
            .ToArray();

        if (userIds.Length == 0)
            return new List<TimetasticHolidayDto>();

        var userIdsCsv = string.Join(",", userIds);
        var firstUrl =
            $"{new Uri(baseUri, "holidays")}?" +
            $"Start={start:yyyy-MM-dd}&End={end:yyyy-MM-dd}&UserIds={Uri.EscapeDataString(userIdsCsv)}&Status=2&NonArchivedUsersOnly=true";

        var holidays = new List<TimetasticHolidayDto>();
        var next = firstUrl;
        var pageGuard = 0;
        while (!string.IsNullOrWhiteSpace(next) && pageGuard < 100)
        {
            pageGuard++;
            using var request = BuildRequest(new Uri(next), token);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Timetastic holidays request failed ({(int)response.StatusCode}): {body}");

            var parsed = ParseHolidayResponse(body);
            holidays.AddRange(parsed.Holidays);
            next = parsed.NextPageLink;
        }

        return holidays;
    }

    private static HolidayPage ParseHolidayResponse(string body)
    {
        using var document = JsonDocument.Parse(body);
        if (document.RootElement.ValueKind == JsonValueKind.Array)
        {
            var flat = JsonSerializer.Deserialize<List<TimetasticHolidayDto>>(body, JsonOptions) ?? new List<TimetasticHolidayDto>();
            return new HolidayPage(flat, null);
        }

        if (document.RootElement.ValueKind != JsonValueKind.Object)
            return new HolidayPage(new List<TimetasticHolidayDto>(), null);

        var holidays = new List<TimetasticHolidayDto>();
        if (document.RootElement.TryGetProperty("holidays", out var holidaysElement)
            && holidaysElement.ValueKind == JsonValueKind.Array)
        {
            var parsed = JsonSerializer.Deserialize<List<TimetasticHolidayDto>>(holidaysElement.GetRawText(), JsonOptions);
            if (parsed is not null)
                holidays.AddRange(parsed);
        }

        string? nextPage = null;
        if (document.RootElement.TryGetProperty("nextPageLink", out var nextPageElement)
            && nextPageElement.ValueKind == JsonValueKind.String)
        {
            nextPage = nextPageElement.GetString();
        }

        return new HolidayPage(holidays, nextPage);
    }

    private static MappingResolution ResolveMappings(
        IntegrationSettings settings,
        IReadOnlyCollection<Mechanic> mechanics,
        IReadOnlyCollection<TimetasticUserDto> users)
    {
        var usersById = users.ToDictionary(u => u.Id);
        var activeUsers = users.Where(u => !u.IsArchived).ToList();
        var usersByFullName = activeUsers
            .GroupBy(GetUserFullName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
        var usersByFirstName = activeUsers
            .Where(u => !string.IsNullOrWhiteSpace(u.Firstname))
            .GroupBy(u => u.Firstname.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var resolved = new List<ResolvedTimetasticMapping>();
        var unresolved = new List<string>();

        foreach (var mechanic in mechanics)
        {
            var localMechanicName = (mechanic.Name ?? "").Trim();
            var explicitMapping = settings.TimetasticMechanicMappings
                .FirstOrDefault(m => m.MechanicId == mechanic.Id);

            TimetasticUserDto? matchedUser = null;
            if (explicitMapping is not null)
            {
                var explicitUserId = ParsePositiveInt(explicitMapping.TimetasticUserId);
                if (explicitUserId.HasValue && usersById.TryGetValue(explicitUserId.Value, out var byId))
                    matchedUser = byId;
                else if (!string.IsNullOrWhiteSpace(explicitMapping.TimetasticUserName)
                         && usersByFullName.TryGetValue(explicitMapping.TimetasticUserName.Trim(), out var byName)
                         && byName.Count == 1)
                    matchedUser = byName[0];
                else if (!string.IsNullOrWhiteSpace(explicitMapping.TimetasticUserName)
                         && usersByFirstName.TryGetValue(explicitMapping.TimetasticUserName.Trim(), out var byFirstName)
                         && byFirstName.Count == 1)
                    matchedUser = byFirstName[0];
            }

            if (matchedUser is null)
            {
                if (usersByFullName.TryGetValue(localMechanicName, out var byName) && byName.Count == 1)
                    matchedUser = byName[0];
            }

            if (matchedUser is null)
            {
                var localFirstName = ExtractFirstToken(localMechanicName);
                if (!string.IsNullOrWhiteSpace(localFirstName)
                    && usersByFirstName.TryGetValue(localFirstName, out var byFirstName)
                    && byFirstName.Count == 1)
                {
                    matchedUser = byFirstName[0];
                }
            }

            if (matchedUser is null)
            {
                unresolved.Add(localMechanicName);
                continue;
            }

            resolved.Add(new ResolvedTimetasticMapping
            {
                MechanicId = mechanic.Id,
                StoreId = mechanic.StoreId,
                MechanicName = localMechanicName,
                TimetasticUserId = matchedUser.Id,
                TimetasticUserName = GetUserFullName(matchedUser)
            });
        }

        return new MappingResolution(resolved, unresolved);
    }

    private static bool TryMapHolidayToTimeOff(
        TimetasticHolidayDto holiday,
        ResolvedTimetasticMapping mapping,
        out MechanicTimeOff entry)
    {
        entry = new MechanicTimeOff();

        if (!holiday.Id.HasValue || !holiday.UserId.HasValue)
            return false;

        return TimetasticTimeOffMapper.TryMapToTimeOff(
            sourceId: holiday.Id.Value,
            startDateRaw: holiday.StartDate,
            startTypeRaw: holiday.StartType,
            endDateRaw: holiday.EndDate,
            endTypeRaw: holiday.EndType,
            bookingUnitRaw: holiday.BookingUnit,
            leaveTypeRaw: holiday.Leavetype,
            reasonRaw: holiday.Reason,
            storeId: mapping.StoreId,
            mechanicId: mapping.MechanicId,
            tenantId: 0,
            includeTenantId: false,
            out entry);
    }

    private static string GetUserFullName(TimetasticUserDto user)
    {
        var first = (user.Firstname ?? "").Trim();
        var last = (user.Surname ?? "").Trim();
        return string.Join(" ", new[] { first, last }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
    }

    private static int? ParsePositiveInt(string? value)
    {
        if (!int.TryParse((value ?? "").Trim(), out var parsed))
            return null;
        return parsed > 0 ? parsed : null;
    }

    private static string ExtractFirstToken(string? value)
    {
        var normalized = (value ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return "";

        var parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 0 ? "" : parts[0];
    }

    private static HttpRequestMessage BuildRequest(Uri uri, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed record HolidayPage(List<TimetasticHolidayDto> Holidays, string? NextPageLink);

    private sealed class MappingResolution
    {
        public MappingResolution(List<ResolvedTimetasticMapping> resolved, List<string> unresolvedMechanicNames)
        {
            Resolved = resolved;
            UnresolvedMechanicNames = unresolvedMechanicNames;
        }

        public List<ResolvedTimetasticMapping> Resolved { get; }
        public List<string> UnresolvedMechanicNames { get; }
    }

    private sealed class ResolvedTimetasticMapping
    {
        public int MechanicId { get; set; }
        public int StoreId { get; set; }
        public string MechanicName { get; set; } = "";
        public int TimetasticUserId { get; set; }
        public string TimetasticUserName { get; set; } = "";
    }

    private sealed class TimetasticUserDto
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("firstname")] public string Firstname { get; set; } = "";
        [JsonPropertyName("surname")] public string Surname { get; set; } = "";
        [JsonPropertyName("isArchived")] public bool IsArchived { get; set; }
    }

    private sealed class TimetasticHolidayDto
    {
        [JsonPropertyName("id")] public int? Id { get; set; }
        [JsonPropertyName("startDate")] public string StartDate { get; set; } = "";
        [JsonPropertyName("startType")] public string StartType { get; set; } = "";
        [JsonPropertyName("endDate")] public string EndDate { get; set; } = "";
        [JsonPropertyName("endType")] public string EndType { get; set; } = "";
        [JsonPropertyName("userId")] public int? UserId { get; set; }
        [JsonPropertyName("status")] public string Status { get; set; } = "";
        [JsonPropertyName("bookingUnit")] public string BookingUnit { get; set; } = "";
        [JsonPropertyName("leaveType")] public string Leavetype { get; set; } = "";
        [JsonPropertyName("reason")] public string Reason { get; set; } = "";
    }
}

public sealed class TimetasticSyncResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public int CreatedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int DeletedCount { get; set; }
    public int ConsideredHolidayCount { get; set; }
    public int ResolvedMappingCount { get; set; }
    public List<string> UnresolvedMechanics { get; set; } = new();

    public static TimetasticSyncResult Failed(string message)
        => new()
        {
            Success = false,
            Message = message
        };
}
