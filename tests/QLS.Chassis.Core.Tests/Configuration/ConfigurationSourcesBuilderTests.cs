using Microsoft.Extensions.Configuration;
using QLS.Chassis.Core.Configuration;

namespace QLS.Chassis.Core.Tests.Configuration;

[Collection("EnvironmentVariables")]
public sealed class ConfigurationSourcesBuilderTests
{
    [Fact]
    public void Build_ReturnsNonNullConfigurations()
    {
        var result = ConfigurationSourcesBuilder.Build();

        Assert.NotNull(result.AppSettingsConfiguration);
        Assert.NotNull(result.MappingConfiguration);
    }

    [Fact]
    public void Build_ContainsBootstrapValuesInMapping()
    {
        var bootstrap = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["BOOT_KEY"] = "boot" })
            .Build();

        var result = ConfigurationSourcesBuilder.Build(bootstrap);

        Assert.Equal("boot", result.BootstrapConfiguration?["BOOT_KEY"]);
        Assert.Equal("boot", result.MappingConfiguration["BOOT_KEY"]);
    }

    [Fact]
    public void Build_MapsEnvironmentVariables()
    {
        const string key = "CHASSIS_CORE_TEST_ENV_KEY";
        Environment.SetEnvironmentVariable(key, "value");
        try
        {
            var result = ConfigurationSourcesBuilder.Build();
            Assert.Equal("value", result.MappingConfiguration[key]);
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, null);
        }
    }
}

[CollectionDefinition("EnvironmentVariables", DisableParallelization = true)]
public sealed class EnvironmentVariablesCollection;
