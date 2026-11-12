using QLS.Chassis.Core.Resilience;

namespace QLS.Chassis.WebApi.Data;

/// <summary>
/// Ready-made resilience policies for
/// <see cref="Extensions.DatabaseServiceCollectionExtensions.AddChassisPostgresDatabaseResiliency"/>.
/// </summary>
public static class DatabaseResiliencePolicies
{
    /// <summary>
    /// QLS.Chassis.Core's default policy for a dependency named <c>postgres</c>, from
    /// <see cref="ResilienceDefaults.For(string)"/>.
    /// </summary>
    /// <remarks>
    /// With the default profile: a 2.5-second timeout per attempt, 2 retries starting at 200 ms, and a circuit that opens
    /// at a 50% failure rate over at least 10 calls, for 30 seconds. The value is created once, when this class is first
    /// used, from <see cref="ResilienceDefaults.Profile"/> at that moment.
    /// </remarks>
    public static readonly ResilienceOptions Default = ResilienceDefaults.For("postgres");
}
