using Microsoft.EntityFrameworkCore;
using Workshop.Data;
using Workshop.Models;

namespace Workshop.Services;

public sealed class TimetasticWebhookService
{
    private readonly IDbContextFactory<WorkshopDbContext> _dbFactory;
    private readonly TimetasticWebhookPayloadParser _payloadParser;
    private readonly ILogger<TimetasticWebhookService> _logger;

    public TimetasticWebhookService(
        IDbContextFactory<WorkshopDbContext> dbFactory,
        TimetasticWebhookPayloadParser payloadParser,
        ILogger<TimetasticWebhookService> logger)
    {
        _dbFactory = dbFactory;
        _payloadParser = payloadParser;
        _logger = logger;
    }

    public async Task<TimetasticWebhookProcessResult> ProcessAsync(
        string? secretHeader,
        string? body,
        CancellationToken cancellationToken = default)
    {
        var secret = (secretHeader ?? "").Trim();
        if (string.IsNullOrWhiteSpace(secret))
            return TimetasticWebhookProcessResult.Unauthorized("Missing Timetastic-Secret header.");

        var payloadRaw = body ?? "";
        if (string.IsNullOrWhiteSpace(payloadRaw))
            return TimetasticWebhookProcessResult.BadRequest("Webhook payload is empty.");

        TimetasticWebhookPayload? payload;
        try
        {
            payload = _payloadParser.Parse(payloadRaw);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse Timetastic webhook payload.");
            return TimetasticWebhookProcessResult.BadRequest("Webhook payload is invalid JSON.");
        }

        if (payload is null || !payload.EventId.HasValue || payload.EventId.Value <= 0)
            return TimetasticWebhookProcessResult.BadRequest("Webhook payload is missing eventId.");

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var enabledSettings = await db.IntegrationSettings
            .Where(x => x.TimetasticEnabled)
            .ToListAsync(cancellationToken);

        var resolution = TimetasticWebhookTenantResolver.ResolveBySecret(secret, enabledSettings);
        if (resolution.IsNotFound)
            return TimetasticWebhookProcessResult.Unauthorized("Invalid Timetastic webhook secret.");
        if (resolution.IsAmbiguous)
            return TimetasticWebhookProcessResult.BadRequest("Webhook secret matches multiple enabled tenants. Configure unique webhook secrets per tenant.");

        var settings = resolution.Settings!;

        var tenantId = settings.TenantId;
        var eventId = payload.EventId.Value;
        var alreadyHandled = await db.TimetasticWebhookEvents
            .AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId && x.EventId == eventId, cancellationToken);

        if (alreadyHandled)
        {
            settings.TimetasticLastWebhookReceivedUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return TimetasticWebhookProcessResult.Ok("Duplicate webhook event ignored.");
        }

        string outcome;
        try
        {
            outcome = await ApplyEventAsync(db, settings, payload, cancellationToken);
            settings.TimetasticLastWebhookReceivedUtc = DateTime.UtcNow;
            db.TimetasticWebhookEvents.Add(new TimetasticWebhookEvent
            {
                TenantId = tenantId,
                EventId = eventId,
                EventType = (payload.EventType ?? "").Trim(),
                ReceivedUtc = DateTime.UtcNow,
                ProcessedUtc = DateTime.UtcNow,
                Outcome = outcome
            });
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateEvent(ex))
        {
            _logger.LogInformation(ex, "Duplicate Timetastic webhook event {EventId} for tenant {TenantId}.", eventId, tenantId);
            return TimetasticWebhookProcessResult.Ok("Duplicate webhook event ignored.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Timetastic webhook processing failed for tenant {TenantId}.", tenantId);
            return TimetasticWebhookProcessResult.Failed("Webhook processing failed.");
        }

        return TimetasticWebhookProcessResult.Ok(outcome);
    }

    private static async Task<string> ApplyEventAsync(
        WorkshopDbContext db,
        IntegrationSettings settings,
        TimetasticWebhookPayload payload,
        CancellationToken cancellationToken)
    {
        var eventType = (payload.EventType ?? "").Trim();
        if (TimetasticEventTypePolicy.IsTest(eventType))
            return "Test event acknowledged.";

        if (TimetasticEventTypePolicy.ShouldUpsert(eventType))
        {
            return await UpsertAbsenceAsync(db, settings, payload, cancellationToken);
        }

        if (TimetasticEventTypePolicy.ShouldDelete(eventType))
        {
            return await DeleteAbsenceAsync(db, settings.TenantId, payload);
        }

        return $"Ignored unsupported event type '{eventType}'.";
    }

    private static async Task<string> UpsertAbsenceAsync(
        WorkshopDbContext db,
        IntegrationSettings settings,
        TimetasticWebhookPayload payload,
        CancellationToken cancellationToken)
    {
        if (!TryGetRecordId(payload, out var recordId) || payload.RecordData is null)
            return "Ignored absence event: record data missing.";

        var mechanic = await ResolveMechanicAsync(db, settings, payload.RecordData, cancellationToken);
        if (mechanic is null)
            return "Ignored absence event: no mapped mechanic.";

        if (!TryMapAbsenceToTimeOff(payload.RecordData, mechanic, recordId, out var mapped))
            return "Ignored absence event: could not parse dates.";

        var existing = await db.MechanicTimeOffEntries
            .FirstOrDefaultAsync(
                x => x.TenantId == settings.TenantId
                     && x.Source == "Timetastic"
                     && x.ExternalId == mapped.ExternalId,
                cancellationToken);

        if (existing is null)
        {
            db.MechanicTimeOffEntries.Add(mapped);
            return $"Created time off entry for event {payload.EventId}.";
        }

        existing.StoreId = mapped.StoreId;
        existing.MechanicId = mapped.MechanicId;
        existing.Type = mapped.Type;
        existing.Start = mapped.Start;
        existing.End = mapped.End;
        existing.IsAllDay = mapped.IsAllDay;
        existing.Notes = mapped.Notes;
        existing.LastSyncedUtc = mapped.LastSyncedUtc;
        return $"Updated time off entry for event {payload.EventId}.";
    }

    private static async Task<string> DeleteAbsenceAsync(
        WorkshopDbContext db,
        int tenantId,
        TimetasticWebhookPayload payload)
    {
        if (!TryGetRecordId(payload, out var recordId))
            return "Ignored delete event: record id missing.";

        var externalId = TimetasticTimeOffIdentity.BuildExternalId(recordId);
        var existing = await db.MechanicTimeOffEntries
            .Where(x => x.TenantId == tenantId
                        && x.Source == "Timetastic"
                        && x.ExternalId == externalId)
            .ToListAsync();

        if (existing.Count == 0)
            return $"No matching time off entry found for {externalId}.";

        db.MechanicTimeOffEntries.RemoveRange(existing);
        return existing.Count == 1
            ? $"Deleted time off entry for {externalId}."
            : $"Deleted {existing.Count} time off entries for {externalId}.";
    }

    private static async Task<Mechanic?> ResolveMechanicAsync(
        WorkshopDbContext db,
        IntegrationSettings settings,
        TimetasticWebhookRecordData record,
        CancellationToken cancellationToken)
    {
        var mechanics = await db.Mechanics
            .Where(x => x.TenantId == settings.TenantId)
            .ToListAsync(cancellationToken);

        if (mechanics.Count == 0)
            return null;

        var mechanicById = mechanics.ToDictionary(x => x.Id);
        var userId = ParsePositiveInt(record.UserId);
        if (userId.HasValue)
        {
            var mappingByUserId = settings.TimetasticMechanicMappings
                .FirstOrDefault(m => ParsePositiveInt(m.TimetasticUserId) == userId.Value);
            if (mappingByUserId is not null
                && mechanicById.TryGetValue(mappingByUserId.MechanicId, out var mappedMechanicByUserId))
            {
                return mappedMechanicByUserId;
            }
        }

        var userName = (record.UserName ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(userName))
        {
            var mappingByUserName = settings.TimetasticMechanicMappings
                .FirstOrDefault(m => string.Equals((m.TimetasticUserName ?? "").Trim(), userName, StringComparison.OrdinalIgnoreCase));
            if (mappingByUserName is not null
                && mechanicById.TryGetValue(mappingByUserName.MechanicId, out var mappedMechanicByUserName))
            {
                return mappedMechanicByUserName;
            }

            var directMatches = mechanics
                .Where(m => string.Equals((m.Name ?? "").Trim(), userName, StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToList();
            if (directMatches.Count == 1)
                return directMatches[0];
        }

        return null;
    }

    private static bool TryMapAbsenceToTimeOff(
        TimetasticWebhookRecordData source,
        Mechanic mechanic,
        int recordId,
        out MechanicTimeOff mapped)
    {
        return TimetasticTimeOffMapper.TryMapToTimeOff(
            sourceId: recordId,
            startDateRaw: source.StartDate,
            startTypeRaw: source.StartType,
            endDateRaw: source.EndDate,
            endTypeRaw: source.EndType,
            bookingUnitRaw: source.BookingUnit,
            leaveTypeRaw: source.Leavetype,
            reasonRaw: source.Reason,
            storeId: mechanic.StoreId,
            mechanicId: mechanic.Id,
            tenantId: mechanic.TenantId,
            includeTenantId: true,
            out mapped);
    }

    private static bool TryGetRecordId(TimetasticWebhookPayload payload, out int recordId)
    {
        recordId = 0;

        if (payload.RecordId.HasValue && payload.RecordId.Value > 0)
        {
            recordId = payload.RecordId.Value;
            return true;
        }

        if (payload.RecordData is not null && payload.RecordData.Id > 0)
        {
            recordId = payload.RecordData.Id;
            return true;
        }

        return false;
    }

    private static int? ParsePositiveInt(string? value)
    {
        if (!int.TryParse((value ?? "").Trim(), out var parsed))
            return null;
        return parsed > 0 ? parsed : null;
    }

    private static bool IsDuplicateEvent(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase);
    }

}

public sealed class TimetasticWebhookProcessResult
{
    public int StatusCode { get; init; }
    public string Message { get; init; } = "";

    public static TimetasticWebhookProcessResult Ok(string message) => new() { StatusCode = 200, Message = message };
    public static TimetasticWebhookProcessResult BadRequest(string message) => new() { StatusCode = 400, Message = message };
    public static TimetasticWebhookProcessResult Unauthorized(string message) => new() { StatusCode = 401, Message = message };
    public static TimetasticWebhookProcessResult Failed(string message) => new() { StatusCode = 500, Message = message };
}
