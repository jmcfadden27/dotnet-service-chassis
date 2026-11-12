namespace QLS.Chassis.Core.Configuration;

/// <summary>
/// Reads typed configuration values. A key counts as missing when no source has a non-blank value for it;
/// see <see cref="ConfigReader"/> for the order in which sources are checked.
/// </summary>
public interface IConfigReader
{
    /// <summary>
    /// Reads a string value that must be present.
    /// </summary>
    /// <param name="key">The configuration key, for example <c>Orders:QueueUrl</c>.</param>
    /// <returns>The value.</returns>
    /// <exception cref="ConfigurationException">The key is missing.</exception>
    string GetRequiredString(string key);
    /// <summary>
    /// Reads a string value, or returns a default when the key is missing.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="defaultValue">The value to return when the key is missing.</param>
    /// <returns>The value, or <paramref name="defaultValue"/>.</returns>
    string GetStringOrDefault(string key, string defaultValue);
    /// <summary>
    /// Reads a 32-bit integer, or returns a default when the key is missing.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="defaultValue">The value to return when the key is missing.</param>
    /// <returns>The parsed value, or <paramref name="defaultValue"/>.</returns>
    /// <exception cref="ConfigurationException">The value is present but is not a valid integer.</exception>
    int GetIntOrDefault(string key, int defaultValue);
    /// <summary>
    /// Reads a 64-bit integer, or returns a default when the key is missing.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="defaultValue">The value to return when the key is missing.</param>
    /// <returns>The parsed value, or <paramref name="defaultValue"/>.</returns>
    /// <exception cref="ConfigurationException">The value is present but is not a valid long.</exception>
    long GetLongOrDefault(string key, long defaultValue);
    /// <summary>
    /// Reads a boolean (<c>true</c> or <c>false</c>, in any case), or returns a default when the key is missing.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="defaultValue">The value to return when the key is missing.</param>
    /// <returns>The parsed value, or <paramref name="defaultValue"/>.</returns>
    /// <exception cref="ConfigurationException">The value is present but is not <c>true</c> or <c>false</c>.</exception>
    bool GetBoolOrDefault(string key, bool defaultValue);
    /// <summary>
    /// Reads a comma-separated list into a set, trimming each entry and dropping empty ones.
    /// A value made only of separators (for example <c>","</c>) gives an empty set, not the defaults.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="defaultValues">The entries to return when the key is missing.</param>
    /// <param name="comparer">The set's comparer. Defaults to <see cref="StringComparer.OrdinalIgnoreCase"/>.</param>
    /// <returns>A new set of the parsed entries, or of <paramref name="defaultValues"/>.</returns>
    HashSet<string> GetCsvSetOrDefault(string key, IEnumerable<string> defaultValues, StringComparer? comparer = null);
}
