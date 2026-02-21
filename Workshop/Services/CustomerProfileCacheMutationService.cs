using Workshop.Models;

namespace Workshop.Services;

public static class CustomerProfileCacheMutationService
{
    public static List<CustomerProfile> UpsertAndSort(
        List<CustomerProfile> profiles,
        CustomerProfile profile)
    {
        var clone = CustomerProfileCoreService.CloneCustomerProfile(profile);
        var index = profiles.FindIndex(c => c.Id == clone.Id);
        if (index >= 0)
            profiles[index] = clone;
        else
            profiles.Add(clone);

        return profiles
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .ThenBy(c => c.AccountNumber)
            .ToList();
    }
}
