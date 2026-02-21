namespace Workshop.Services;

public static class TimetasticEventTypePolicy
{
    public static bool IsTest(string? eventType) =>
        EqualsEvent(eventType, "TestEvent");

    public static bool ShouldUpsert(string? eventType) =>
        EqualsEvent(eventType, "AbsenceBooked")
        || EqualsEvent(eventType, "AbsenceApproved");

    public static bool ShouldDelete(string? eventType) =>
        EqualsEvent(eventType, "AbsenceCancelled")
        || EqualsEvent(eventType, "AbsenceDeclined");

    public static bool IsSupported(string? eventType) =>
        IsTest(eventType) || ShouldUpsert(eventType) || ShouldDelete(eventType);

    private static bool EqualsEvent(string? left, string right) =>
        string.Equals((left ?? "").Trim(), right, StringComparison.OrdinalIgnoreCase);
}
