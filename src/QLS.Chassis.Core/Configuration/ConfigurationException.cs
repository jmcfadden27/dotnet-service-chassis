namespace QLS.Chassis.Core.Configuration;

/// <summary>
/// Thrown when a configuration key is missing or its value cannot be parsed.
/// </summary>
public sealed class ConfigurationException : Exception
{
    /// <summary>
    /// The configuration key that failed, so start-up errors name the setting to fix.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Creates the exception for a key.
    /// </summary>
    /// <param name="key">The configuration key that failed.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The underlying exception, if any.</param>
    public ConfigurationException(string key, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Key = key;
    }
}
