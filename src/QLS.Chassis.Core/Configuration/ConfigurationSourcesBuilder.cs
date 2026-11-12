using Microsoft.Extensions.Configuration;

namespace QLS.Chassis.Core.Configuration;

/// <summary>
/// Builds <see cref="ConfigurationSources"/> from the application's appsettings files, an optional bootstrap configuration and environment variables.
/// </summary>
public static class ConfigurationSourcesBuilder
{
    /// <summary>
    /// Loads <c>appsettings.json</c> and <c>appsettings.{environment}.json</c> (both optional, not reloaded on change) from
    /// <see cref="AppContext.BaseDirectory"/>. The environment comes from <c>DOTNET_ENVIRONMENT</c>, else
    /// <c>ASPNETCORE_ENVIRONMENT</c>, else <c>Production</c>.
    /// </summary>
    /// <param name="bootstrapConfiguration">Extra configuration layered between the JSON files and environment variables, or <see langword="null"/>.</param>
    /// <returns>The three configuration views.</returns>
    public static ConfigurationSources Build(IConfiguration? bootstrapConfiguration = null)
    {
        var environmentName =
            Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "Production";

        var appSettingsConfiguration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: false)
            .Build();

        var mappingBuilder = new ConfigurationBuilder()
            .AddConfiguration(appSettingsConfiguration);

        if (bootstrapConfiguration != null)
            mappingBuilder.AddConfiguration(bootstrapConfiguration);

        mappingBuilder.AddEnvironmentVariables();

        return new ConfigurationSources
        {
            AppSettingsConfiguration = appSettingsConfiguration,
            BootstrapConfiguration = bootstrapConfiguration,
            MappingConfiguration = mappingBuilder.Build(),
        };
    }
}
