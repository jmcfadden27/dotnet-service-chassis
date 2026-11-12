namespace QLS.Chassis.Core.Resilience;

/// <summary>
/// Builds <see cref="ResilienceOptions"/> for a dependency from a <see cref="ResilienceDefaultsProfile"/>.
/// </summary>
public static class ResilienceDefaults
{
    /// <summary>
    /// The profile used by <see cref="For(string)"/>. Assign a subclass at start-up to change the defaults everywhere.
    /// </summary>
    public static ResilienceDefaultsProfile Profile { get; set; } = new ResilienceDefaultsProfile();

    /// <summary>
    /// Creates options for a dependency from <see cref="Profile"/>.
    /// </summary>
    /// <param name="dependencyName">The dependency name, which selects the circuit.</param>
    /// <returns>The options.</returns>
    /// <exception cref="ArgumentException"><paramref name="dependencyName"/> is null, empty or whitespace.</exception>
    public static ResilienceOptions For(string dependencyName)
    {
        return For(dependencyName, Profile);
    }

    /// <summary>
    /// Creates options for a dependency from the given profile. Out-of-range values are corrected rather than rejected:
    /// non-positive timeouts, delays, throughput and seconds, and a negative retry count, fall back to the built-in defaults;
    /// a failure ratio of 0 or below becomes 0.1, 1 or above becomes 1, and NaN or infinity becomes 0.5.
    /// </summary>
    /// <param name="dependencyName">The dependency name, which selects the circuit.</param>
    /// <param name="profile">The profile to take values from.</param>
    /// <returns>The options.</returns>
    /// <exception cref="ArgumentException"><paramref name="dependencyName"/> is null, empty or whitespace.</exception>
    public static ResilienceOptions For(string dependencyName, ResilienceDefaultsProfile profile)
    {
        if (string.IsNullOrWhiteSpace(dependencyName))
            throw new ArgumentException("Dependency name is required.", nameof(dependencyName));

        return new ResilienceOptions
        {
            DependencyName = dependencyName,
            TimeoutMs = NormalizePositiveInt(profile.TimeoutMs, 2500),
            MaxRetries = NormalizeNonNegativeInt(profile.MaxRetries, 2),
            BaseDelayMs = NormalizePositiveInt(profile.BaseDelayMs, 200),
            CircuitFailureRatio = NormalizeRatio(profile.CircuitFailureRatio),
            CircuitMinThroughput = NormalizePositiveInt(profile.CircuitMinThroughput, 10),
            CircuitBreakSeconds = NormalizePositiveInt(profile.CircuitBreakSeconds, 30),
            RetryAfterSeconds = NormalizePositiveInt(profile.RetryAfterSeconds, 30),
        };
    }

    private static int NormalizePositiveInt(int value, int fallback)
    {
        return value > 0 ? value : fallback;
    }

    private static int NormalizeNonNegativeInt(int value, int fallback)
    {
        return value >= 0 ? value : fallback;
    }

    private static double NormalizeRatio(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return 0.5d;

        if (value <= 0d)
            return 0.1d;

        if (value >= 1d)
            return 1d;

        return value;
    }
}
