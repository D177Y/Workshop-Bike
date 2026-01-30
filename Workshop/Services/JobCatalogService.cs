using Workshop.Models;

namespace Workshop.Services;

public sealed class JobCatalogService
{
    public List<JobDefinition> Jobs { get; } = new()
    {
        new JobDefinition { Id = "SVC_BRONZE", Name = "Bronze service", DefaultMinutes = 60, BasePriceIncVat = 79.00m },
        new JobDefinition { Id = "SVC_SILVER", Name = "Silver service", DefaultMinutes = 75, BasePriceIncVat = 109.00m },
        new JobDefinition { Id = "SVC_GOLD", Name = "Gold service", DefaultMinutes = 90, BasePriceIncVat = 149.00m },
    };

    public List<AddOnDefinition> AddOns { get; } = new()
    {
        new AddOnDefinition { Id = "ADD_CHAIN", Name = "Replace chain" },
        new AddOnDefinition { Id = "ADD_BLEED", Name = "Brake bleed" },
        new AddOnDefinition { Id = "ADD_TUBE", Name = "Replace inner tube" },
    };

    // Rules: same add-on can have different minutes/price depending on the base job
    public List<AddOnRule> Rules { get; } = new()
    {
        // Chain: adds time on Bronze, less on Silver, none on Gold (example rule)
        new AddOnRule { JobId = "SVC_BRONZE", AddOnId = "ADD_CHAIN", ExtraMinutes = 5, ExtraPriceIncVat = 10.00m },
        new AddOnRule { JobId = "SVC_SILVER", AddOnId = "ADD_CHAIN", ExtraMinutes = 0, ExtraPriceIncVat = 6.00m },
        new AddOnRule { JobId = "SVC_GOLD", AddOnId = "ADD_CHAIN", ExtraMinutes = 0, ExtraPriceIncVat = 5.00m },

        // Brake bleed
        new AddOnRule { JobId = "SVC_BRONZE", AddOnId = "ADD_BLEED", ExtraMinutes = 30, ExtraPriceIncVat = 35.00m },
        new AddOnRule { JobId = "SVC_SILVER", AddOnId = "ADD_BLEED", ExtraMinutes = 25, ExtraPriceIncVat = 35.00m },
        new AddOnRule { JobId = "SVC_GOLD", AddOnId = "ADD_BLEED", ExtraMinutes = 20, ExtraPriceIncVat = 35.00m },

        // Tube
        new AddOnRule { JobId = "SVC_BRONZE", AddOnId = "ADD_TUBE", ExtraMinutes = 15, ExtraPriceIncVat = 15.00m },
        new AddOnRule { JobId = "SVC_SILVER", AddOnId = "ADD_TUBE", ExtraMinutes = 10, ExtraPriceIncVat = 15.00m },
        new AddOnRule { JobId = "SVC_GOLD", AddOnId = "ADD_TUBE", ExtraMinutes = 5, ExtraPriceIncVat = 15.00m },
    };

    public (int minutes, decimal priceIncVat, string title) PriceAndTime(string jobId, IReadOnlyCollection<string> addOnIds)
    {
        var job = Jobs.First(x => x.Id == jobId);

        var minutes = job.DefaultMinutes;
        var price = job.BasePriceIncVat;

        foreach (var addOnId in addOnIds)
        {
            var rule = Rules.FirstOrDefault(r => r.JobId == jobId && r.AddOnId == addOnId);
            if (rule is null) continue;

            minutes += rule.ExtraMinutes;
            price += rule.ExtraPriceIncVat;
        }

        var title = job.Name;
        return (minutes, price, title);
    }
}
