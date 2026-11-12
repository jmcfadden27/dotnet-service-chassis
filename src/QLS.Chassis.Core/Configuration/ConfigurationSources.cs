using Microsoft.Extensions.Configuration;

namespace QLS.Chassis.Core.Configuration;

/// <summary>
/// The configuration views built by <see cref="ConfigurationSourcesBuilder.Build"/> and read by <see cref="ConfigReader"/>.
/// </summary>
public sealed class ConfigurationSources
{
    /// <summary>
    /// Only the JSON files: <c>appsettings.json</c> and <c>appsettings.{environment}.json</c>.
    /// </summary>
    public IConfiguration AppSettingsConfiguration { get; init; } = null!;
    /// <summary>
    /// The bootstrap configuration passed to the builder (for example values loaded from a secrets store), or <see langword="null"/>.
    /// </summary>
    public IConfiguration? BootstrapConfiguration { get; init; }
    /// <summary>
    /// The JSON files, then the bootstrap configuration, then environment variables, with later sources winning.
    /// Use it to bind option classes when environment variables should override appsettings.
    /// </summary>
    public IConfiguration MappingConfiguration { get; init; } = null!;
}
