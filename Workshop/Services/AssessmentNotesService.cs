namespace Workshop.Services;

public static class AssessmentNotesService
{
    public static string BuildBookingNotes(IEnumerable<string> notes)
    {
        var orderedNotes = notes
            .Select(n => (n ?? "").Trim())
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToList();

        if (orderedNotes.Count == 0)
            return "";

        var numbered = orderedNotes
            .Select((note, index) => $"{index + 1}. {note}")
            .ToList();

        var text = string.Join("\n", numbered);
        if (text.Length > 1400)
            text = text[..1400] + "...";

        return text;
    }
}
