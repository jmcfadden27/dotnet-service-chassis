namespace QLS.Chassis.Core.Configuration;

/// <summary>
/// Reads environment variables. It exists so tests can supply values without touching the real process environment.
/// </summary>
public interface IEnvironmentProvider
{
    /// <summary>
    /// Gets an environment variable.
    /// </summary>
    /// <param name="key">The variable name.</param>
    /// <returns>The value, or <see langword="null"/> when it isn't set.</returns>
    string? Get(string key);
    /// <summary>
    /// Gets an environment variable, treating an unset or blank value as missing.
    /// </summary>
    /// <param name="key">The variable name.</param>
    /// <param name="value">The value when found; otherwise an empty string.</param>
    /// <returns><see langword="true"/> when the variable is set and not blank.</returns>
    bool TryGet(string key, out string value);
}
