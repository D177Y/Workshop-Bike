namespace Workshop.Models;

public enum ServicePricingMode
{
    AutoRate = 0,
    FixedPrice = 1,
    EstimatedPrice = 2
}

public enum ServiceHourlyRateTier
{
    Default = 0,
    Discounted = 1,
    LossLeader = 2
}
