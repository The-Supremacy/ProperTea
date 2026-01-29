namespace ProperTea.Organization.Features.Organizations.Policies;

/// <summary>
/// Business policy for subscription tier limits and capabilities.
/// Enforces rules that span multiple aggregates (e.g., property count limits).
/// </summary>
public class SubscriptionLimitPolicy
{
    // TODO: Implement subscription limit checks
    // Example: CanAddProperty(org) - checks if org can add more properties based on tier
    // Requires querying property count from projections/other aggregates
}
