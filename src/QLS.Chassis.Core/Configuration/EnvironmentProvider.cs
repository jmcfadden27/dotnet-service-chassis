namespace QLS.Chassis.Core.Configuration;

/// <summary>
/// Default <see cref="IEnvironmentProvider"/>, reading the real process environment variables.
/// </summary>
public sealed class EnvironmentProvider : IEnvironmentProvider
{
    /// <inheritdoc />
    public string? Get(string key)
    {
        return Environment.GetEnvironmentVariable(key);
    }

    /// <inheritdoc />
    public bool TryGet(string key, out string value)
    {
        var rawValue = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            value = string.Empty;
            return false;
        }

        value = rawValue;
        return true;
    }
}
