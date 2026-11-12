namespace QLS.Chassis.Core.Configuration;

/// <summary>
/// Default <see cref="IConfigReader"/>. Looks a key up in <see cref="ConfigurationSources.AppSettingsConfiguration"/>,
/// then <see cref="ConfigurationSources.BootstrapConfiguration"/>, then environment variables, and uses the first
/// non-blank value, so a value in appsettings wins over an environment variable with the same name.
/// </summary>
public sealed class ConfigReader : IConfigReader
{
    private readonly ConfigurationSources _sources;
    private readonly IEnvironmentProvider _environmentProvider;

    /// <summary>
    /// Creates a reader over the given configuration sources.
    /// </summary>
    /// <param name="sources">The appsettings and bootstrap configuration to read from.</param>
    /// <param name="environmentProvider">Supplies environment variables, the last place a key is looked up.</param>
    public ConfigReader(
        ConfigurationSources sources,
        IEnvironmentProvider environmentProvider)
    {
        _sources = sources;
        _environmentProvider = environmentProvider;
    }

    /// <inheritdoc />
    public string GetRequiredString(string key)
    {
        if (!TryResolveValue(key, out var value))
            throw new ConfigurationException(key, $"Missing required configuration key '{key}'.");

        return value;
    }

    /// <inheritdoc />
    public string GetStringOrDefault(string key, string defaultValue)
    {
        return TryResolveValue(key, out var value) ? value : defaultValue;
    }

    /// <inheritdoc />
    public int GetIntOrDefault(string key, int defaultValue)
    {
        if (!TryResolveValue(key, out var value))
            return defaultValue;

        if (!int.TryParse(value, out var parsed))
            throw new ConfigurationException(key, $"Configuration key '{key}' must be a valid integer.");

        return parsed;
    }

    /// <inheritdoc />
    public long GetLongOrDefault(string key, long defaultValue)
    {
        if (!TryResolveValue(key, out var value))
            return defaultValue;

        if (!long.TryParse(value, out var parsed))
            throw new ConfigurationException(key, $"Configuration key '{key}' must be a valid long.");

        return parsed;
    }

    /// <inheritdoc />
    public bool GetBoolOrDefault(string key, bool defaultValue)
    {
        if (!TryResolveValue(key, out var value))
            return defaultValue;

        if (!bool.TryParse(value, out var parsed))
            throw new ConfigurationException(key, $"Configuration key '{key}' must be a valid boolean.");

        return parsed;
    }

    /// <inheritdoc />
    public HashSet<string> GetCsvSetOrDefault(string key, IEnumerable<string> defaultValues, StringComparer? comparer = null)
    {
        comparer ??= StringComparer.OrdinalIgnoreCase;

        if (!TryResolveValue(key, out var value))
            return new HashSet<string>(defaultValues, comparer);

        var parsed = value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(item => !string.IsNullOrWhiteSpace(item));

        return new HashSet<string>(parsed, comparer);
    }

    private bool TryResolveValue(string key, out string value)
    {
        value = string.Empty;

        var appSettingsValue = _sources.AppSettingsConfiguration[key];
        if (!string.IsNullOrWhiteSpace(appSettingsValue))
        {
            value = appSettingsValue;
            return true;
        }

        var bootstrapValue = _sources.BootstrapConfiguration?[key];
        if (!string.IsNullOrWhiteSpace(bootstrapValue))
        {
            value = bootstrapValue;
            return true;
        }

        if (_environmentProvider.TryGet(key, out value))
            return true;

        value = string.Empty;
        return false;
    }
}
