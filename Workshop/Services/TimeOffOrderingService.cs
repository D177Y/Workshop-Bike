using Workshop.Models;

namespace Workshop.Services;

public static class TimeOffOrderingService
{
    public static List<MechanicTimeOff> OrderBySoonest(IEnumerable<MechanicTimeOff> source)
        => source
            .OrderBy(x => x.Start)
            .ThenBy(x => x.End)
            .ThenBy(x => x.Id)
            .ToList();
}
