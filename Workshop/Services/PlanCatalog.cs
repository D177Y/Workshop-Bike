using Workshop.Models;

namespace Workshop.Services;

public static class PlanCatalog
{
    private static readonly IReadOnlyDictionary<PlanTier, PlanDefinition> DefinitionsByTier =
        new Dictionary<PlanTier, PlanDefinition>
        {
            [PlanTier.Starter] = new(PlanTier.Starter, "starter", "Starter", 1, 1, 1, 15m, 150m, false),
            [PlanTier.Standard] = new(PlanTier.Standard, "standard", "Standard", 3, 6, 5, 25m, 250m, false),
            [PlanTier.Premium] = new(PlanTier.Premium, "premium", "Premium", 10, 30, 15, 45m, 450m, false),
            [PlanTier.Enterprise] = new(PlanTier.Enterprise, "enterprise", "Enterprise", 25, 50, 30, 99m, 990m, true)
        };

    public static IReadOnlyList<PlanDefinition> Ordered => DefinitionsByTier.Values
        .OrderBy(definition => Rank(definition.Tier))
        .ToList();

    public static PlanDefinition Get(PlanTier tier)
        => DefinitionsByTier.TryGetValue(tier, out var definition)
            ? definition
            : DefinitionsByTier[PlanTier.Standard];

    public static int GetMechanicLimit(PlanTier tier) => Get(tier).MechanicLimit;

    public static bool TryParseKey(string? rawKey, out PlanTier tier)
    {
        tier = PlanTier.Standard;
        if (string.IsNullOrWhiteSpace(rawKey))
            return false;

        var normalized = rawKey.Trim().ToLowerInvariant();
        foreach (var definition in DefinitionsByTier.Values)
        {
            if (string.Equals(definition.Key, normalized, StringComparison.Ordinal))
            {
                tier = definition.Tier;
                return true;
            }
        }

        return false;
    }

    public static string ToKey(PlanTier tier) => Get(tier).Key;

    public static int Rank(PlanTier tier) => tier switch
    {
        PlanTier.Starter => 0,
        PlanTier.Standard => 1,
        PlanTier.Premium => 2,
        PlanTier.Enterprise => 3,
        _ => 99
    };
}

public sealed record PlanDefinition(
    PlanTier Tier,
    string Key,
    string Name,
    int StoreLimit,
    int MechanicLimit,
    int UserLimit,
    decimal MonthlyPrice,
    decimal AnnualPrice,
    bool IncludesAbacus);
