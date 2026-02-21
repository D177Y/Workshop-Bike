namespace Workshop.Services;

public static class TimetasticTimeOffIdentity
{
    public static string BuildExternalId(int sourceId) => $"holiday:{sourceId}";
}
