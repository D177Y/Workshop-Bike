using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Workshop.Services;

public sealed class TimetasticWebhookPayloadParser
{
    public TimetasticWebhookPayload? Parse(string payloadRaw)
    {
        var trimmed = (payloadRaw ?? "").Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return null;

        if (TryParseAsJson(trimmed, out var jsonPayload))
            return jsonPayload;

        if (TryParseEmbeddedJson(trimmed, out var embeddedJsonPayload))
            return embeddedJsonPayload;

        if (TryParseAsForm(trimmed, out var formPayload))
            return formPayload;

        return null;
    }

    private static bool TryParseAsJson(string payloadRaw, out TimetasticWebhookPayload? payload)
    {
        payload = null;
        try
        {
            payload = JsonSerializer.Deserialize<TimetasticWebhookPayload>(payloadRaw, JsonOptions);
            if (payload is not null && payload.EventId.HasValue && payload.EventId.Value > 0)
                return true;

            var parsedNode = JsonNode.Parse(payloadRaw);
            JsonObject? node = parsedNode as JsonObject;
            if (parsedNode is JsonArray array && array.Count > 0)
                node = array[0] as JsonObject;

            if (node is null)
                return false;

            var eventId = ReadLong(node, "eventId", "eventID", "event_id", "id");
            var eventType = ReadString(node, "eventType", "event_type", "type");
            var recordId = ReadInt(node, "recordId", "record_id");

            if (!eventId.HasValue || string.IsNullOrWhiteSpace(eventType))
                return false;

            var parsed = new TimetasticWebhookPayload
            {
                EventId = eventId,
                EventType = eventType,
                RecordId = recordId
            };

            var recordNode = ReadObject(node, "recordData", "record_data", "record", "data");
            if (recordNode is not null)
            {
                parsed.RecordData = new TimetasticWebhookRecordData
                {
                    Id = ReadInt(recordNode, "id") ?? 0,
                    StartDate = ReadString(recordNode, "startDate", "start_date") ?? "",
                    StartType = ReadString(recordNode, "startType", "start_type") ?? "",
                    EndDate = ReadString(recordNode, "endDate", "end_date") ?? "",
                    EndType = ReadString(recordNode, "endType", "end_type") ?? "",
                    UserId = ReadString(recordNode, "userId", "user_id") ?? "",
                    UserName = ReadString(recordNode, "userName", "user_name") ?? "",
                    Status = ReadString(recordNode, "status") ?? "",
                    BookingUnit = ReadString(recordNode, "bookingUnit", "booking_unit") ?? "",
                    Leavetype = ReadString(recordNode, "leaveType", "leave_type") ?? "",
                    Reason = ReadString(recordNode, "reason") ?? ""
                };
            }

            payload = parsed;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryParseEmbeddedJson(string payloadRaw, out TimetasticWebhookPayload? payload)
    {
        payload = null;

        var firstObject = payloadRaw.IndexOf('{');
        var lastObject = payloadRaw.LastIndexOf('}');
        if (firstObject >= 0 && lastObject > firstObject)
        {
            var candidate = payloadRaw.Substring(firstObject, lastObject - firstObject + 1);
            if (TryParseAsJson(candidate, out payload))
                return payload is not null;
        }

        var firstArray = payloadRaw.IndexOf('[');
        var lastArray = payloadRaw.LastIndexOf(']');
        if (firstArray >= 0 && lastArray > firstArray)
        {
            var candidate = payloadRaw.Substring(firstArray, lastArray - firstArray + 1);
            if (TryParseAsJson(candidate, out payload))
                return payload is not null;
        }

        return false;
    }

    private static bool TryParseAsForm(string payloadRaw, out TimetasticWebhookPayload? payload)
    {
        payload = null;

        var map = ParseFormEncoded(payloadRaw);
        if (map.Count == 0)
            return false;

        if (map.TryGetValue("payload", out var wrappedJson)
            && !string.IsNullOrWhiteSpace(wrappedJson)
            && TryParseAsJson(wrappedJson, out payload))
        {
            return payload is not null;
        }

        var eventId = TryParseLong(GetValue(map, "eventId"));
        var eventType = GetValue(map, "eventType") ?? "";
        var recordId = TryParseInt(GetValue(map, "recordId"));

        if (!eventId.HasValue || string.IsNullOrWhiteSpace(eventType))
            return false;

        var parsed = new TimetasticWebhookPayload
        {
            EventId = eventId,
            EventType = eventType,
            RecordId = recordId
        };

        var recordDataRaw = GetValue(map, "recordData");
        if (!string.IsNullOrWhiteSpace(recordDataRaw))
        {
            try
            {
                parsed.RecordData = JsonSerializer.Deserialize<TimetasticWebhookRecordData>(recordDataRaw, JsonOptions);
            }
            catch
            {
                // Ignore and attempt flat-field fallback.
            }
        }

        if (parsed.RecordData is null)
        {
            var flatRecord = BuildRecordDataFromFlatForm(map);
            if (flatRecord is not null)
                parsed.RecordData = flatRecord;
        }

        payload = parsed;
        return true;
    }

    private static TimetasticWebhookRecordData? BuildRecordDataFromFlatForm(Dictionary<string, string> map)
    {
        var startDate = GetValue(map, "startDate") ?? "";
        var endDate = GetValue(map, "endDate") ?? "";
        var userId = GetValue(map, "userId") ?? "";
        var userName = GetValue(map, "userName") ?? "";

        if (string.IsNullOrWhiteSpace(startDate)
            && string.IsNullOrWhiteSpace(endDate)
            && string.IsNullOrWhiteSpace(userId)
            && string.IsNullOrWhiteSpace(userName))
        {
            return null;
        }

        return new TimetasticWebhookRecordData
        {
            Id = TryParseInt(GetValue(map, "id")) ?? 0,
            StartDate = startDate,
            StartType = GetValue(map, "startType") ?? "",
            EndDate = endDate,
            EndType = GetValue(map, "endType") ?? "",
            UserId = userId,
            UserName = userName,
            Status = GetValue(map, "status") ?? "",
            BookingUnit = GetValue(map, "bookingUnit") ?? "",
            Leavetype = GetValue(map, "leaveType") ?? "",
            Reason = GetValue(map, "reason") ?? ""
        };
    }

    private static Dictionary<string, string> ParseFormEncoded(string value)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var fragments = value.Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var fragment in fragments)
        {
            var pair = fragment.Split('=', 2);
            var key = Uri.UnescapeDataString((pair[0] ?? "").Replace('+', ' ')).Trim();
            if (string.IsNullOrWhiteSpace(key))
                continue;

            var rawValue = pair.Length > 1 ? pair[1] : "";
            var decoded = Uri.UnescapeDataString(rawValue.Replace('+', ' ')).Trim();
            result[key] = decoded;
        }

        return result;
    }

    private static string? GetValue(Dictionary<string, string> map, string key)
    {
        if (map.TryGetValue(key, out var direct))
            return direct;

        foreach (var kvp in map)
        {
            var normalized = kvp.Key.Trim();
            if (normalized.EndsWith("." + key, StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith("[" + key + "]", StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith("recordData." + key, StringComparison.OrdinalIgnoreCase))
            {
                return kvp.Value;
            }
        }

        return null;
    }

    private static long? TryParseLong(string? value)
    {
        if (!long.TryParse((value ?? "").Trim(), out var parsed))
            return null;
        return parsed > 0 ? parsed : null;
    }

    private static int? TryParseInt(string? value)
    {
        if (!int.TryParse((value ?? "").Trim(), out var parsed))
            return null;
        return parsed > 0 ? parsed : null;
    }

    private static JsonObject? ReadObject(JsonObject node, params string[] names)
    {
        foreach (var name in names)
        {
            foreach (var prop in node)
            {
                if (!prop.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (prop.Value is JsonObject obj)
                    return obj;
            }
        }

        return null;
    }

    private static string? ReadString(JsonObject node, params string[] names)
    {
        foreach (var name in names)
        {
            foreach (var prop in node)
            {
                if (!prop.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
                    continue;

                var text = prop.Value?.ToString()?.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                    return text;
            }
        }

        return null;
    }

    private static long? ReadLong(JsonObject node, params string[] names)
    {
        var text = ReadString(node, names);
        return TryParseLong(text);
    }

    private static int? ReadInt(JsonObject node, params string[] names)
    {
        var text = ReadString(node, names);
        return TryParseInt(text);
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };
}

public sealed class TimetasticWebhookPayload
{
    [JsonPropertyName("eventId")] public long? EventId { get; set; }
    [JsonPropertyName("eventType")] public string EventType { get; set; } = "";
    [JsonPropertyName("recordId")] public int? RecordId { get; set; }
    [JsonPropertyName("recordData")] public TimetasticWebhookRecordData? RecordData { get; set; }
}

public sealed class TimetasticWebhookRecordData
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("startDate")] public string StartDate { get; set; } = "";
    [JsonPropertyName("startType")] public string StartType { get; set; } = "";
    [JsonPropertyName("endDate")] public string EndDate { get; set; } = "";
    [JsonPropertyName("endType")] public string EndType { get; set; } = "";
    [JsonConverter(typeof(StringOrNumberJsonConverter))]
    [JsonPropertyName("userId")] public string UserId { get; set; } = "";
    [JsonPropertyName("userName")] public string UserName { get; set; } = "";
    [JsonPropertyName("status")] public string Status { get; set; } = "";
    [JsonPropertyName("bookingUnit")] public string BookingUnit { get; set; } = "";
    [JsonPropertyName("leaveType")] public string Leavetype { get; set; } = "";
    [JsonPropertyName("reason")] public string Reason { get; set; } = "";
}

public sealed class StringOrNumberJsonConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
            return reader.GetString() ?? "";

        if (reader.TokenType == JsonTokenType.Number)
            return reader.TryGetInt64(out var number) ? number.ToString(CultureInfo.InvariantCulture) : reader.GetDouble().ToString(CultureInfo.InvariantCulture);

        if (reader.TokenType == JsonTokenType.True)
            return "true";

        if (reader.TokenType == JsonTokenType.False)
            return "false";

        if (reader.TokenType == JsonTokenType.Null)
            return "";

        throw new JsonException($"Unsupported token type '{reader.TokenType}' for string/number conversion.");
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        => writer.WriteStringValue(value);
}
