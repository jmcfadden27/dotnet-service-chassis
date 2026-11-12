using Microsoft.Extensions.Configuration;
using QLS.Chassis.Core.Configuration;

namespace QLS.Chassis.Core.Tests.Configuration;

public sealed class ConfigReaderTests
{
    [Fact]
    public void GetRequiredString_UsesPrecedence_AppSettingsThenBootstrapThenEnvironment()
    {
        var reader = CreateReader(
            appSettings: new Dictionary<string, string?> { ["KEY"] = "app" },
            bootstrap: new Dictionary<string, string?> { ["KEY"] = "boot" },
            environment: new Dictionary<string, string?> { ["KEY"] = "env" });

        var value = reader.GetRequiredString("KEY");

        Assert.Equal("app", value);
    }

    [Fact]
    public void GetRequiredString_Throws_WhenMissing()
    {
        var reader = CreateReader();

        var ex = Assert.Throws<ConfigurationException>(() => reader.GetRequiredString("MISSING"));
        Assert.Equal("MISSING", ex.Key);
    }

    [Fact]
    public void GetStringOrDefault_ReturnsDefault_WhenMissing()
    {
        var reader = CreateReader();
        Assert.Equal("fallback", reader.GetStringOrDefault("MISSING", "fallback"));
    }

    [Fact]
    public void GetIntOrDefault_ParsesValue_AndThrowsWhenInvalid()
    {
        var reader = CreateReader(environment: new Dictionary<string, string?> { ["INT"] = "42", ["BAD"] = "x" });

        Assert.Equal(42, reader.GetIntOrDefault("INT", 0));
        Assert.Throws<ConfigurationException>(() => reader.GetIntOrDefault("BAD", 0));
        Assert.Equal(7, reader.GetIntOrDefault("MISSING", 7));
    }

    [Fact]
    public void GetLongOrDefault_ParsesValue_AndThrowsWhenInvalid()
    {
        var reader = CreateReader(environment: new Dictionary<string, string?> { ["LONG"] = "922337203685477580", ["BAD"] = "x" });

        Assert.Equal(922337203685477580, reader.GetLongOrDefault("LONG", 0));
        Assert.Throws<ConfigurationException>(() => reader.GetLongOrDefault("BAD", 0));
        Assert.Equal(9, reader.GetLongOrDefault("MISSING", 9));
    }

    [Fact]
    public void GetBoolOrDefault_ParsesValue_AndThrowsWhenInvalid()
    {
        var reader = CreateReader(environment: new Dictionary<string, string?> { ["BOOL"] = "true", ["BAD"] = "x" });

        Assert.True(reader.GetBoolOrDefault("BOOL", false));
        Assert.Throws<ConfigurationException>(() => reader.GetBoolOrDefault("BAD", false));
        Assert.True(reader.GetBoolOrDefault("MISSING", true));
    }

    [Fact]
    public void GetCsvSetOrDefault_ParsesAndTrims_WhenPresent()
    {
        var reader = CreateReader(environment: new Dictionary<string, string?> { ["CSV"] = " a, b ,,c " });

        var values = reader.GetCsvSetOrDefault("CSV", new[] { "x" });

        Assert.Equal(3, values.Count);
        Assert.Contains("a", values);
        Assert.Contains("b", values);
        Assert.Contains("c", values);
    }

    [Fact]
    public void GetCsvSetOrDefault_UsesDefaults_WhenMissing()
    {
        var reader = CreateReader();

        var values = reader.GetCsvSetOrDefault("CSV", new[] { "a", "b" });

        Assert.Equal(2, values.Count);
        Assert.Contains("a", values);
        Assert.Contains("b", values);
    }

    [Fact]
    public void GetRequiredString_FallsBackToBootstrapThenEnvironment()
    {
        var bootstrapFirst = CreateReader(
            bootstrap: new Dictionary<string, string?> { ["KEY"] = "boot" },
            environment: new Dictionary<string, string?> { ["KEY"] = "env" });
        Assert.Equal("boot", bootstrapFirst.GetRequiredString("KEY"));

        var envOnly = CreateReader(environment: new Dictionary<string, string?> { ["KEY"] = "env" });
        Assert.Equal("env", envOnly.GetRequiredString("KEY"));
    }

    private static ConfigReader CreateReader(
        Dictionary<string, string?>? appSettings = null,
        Dictionary<string, string?>? bootstrap = null,
        Dictionary<string, string?>? environment = null)
    {
        var appConfig = new ConfigurationBuilder().AddInMemoryCollection(appSettings ?? new Dictionary<string, string?>()).Build();
        IConfiguration? bootstrapConfig = bootstrap == null
            ? null
            : new ConfigurationBuilder().AddInMemoryCollection(bootstrap).Build();

        var sources = new ConfigurationSources
        {
            AppSettingsConfiguration = appConfig,
            BootstrapConfiguration = bootstrapConfig,
            MappingConfiguration = new ConfigurationBuilder().Build(),
        };

        var envProvider = new StubEnvironmentProvider(environment ?? new Dictionary<string, string?>());
        return new ConfigReader(sources, envProvider);
    }

    private sealed class StubEnvironmentProvider : IEnvironmentProvider
    {
        private readonly IReadOnlyDictionary<string, string?> _values;

        public StubEnvironmentProvider(IReadOnlyDictionary<string, string?> values)
        {
            _values = values;
        }

        public string? Get(string key)
            => _values.TryGetValue(key, out var value) ? value : null;

        public bool TryGet(string key, out string value)
        {
            if (_values.TryGetValue(key, out var raw) && !string.IsNullOrWhiteSpace(raw))
            {
                value = raw;
                return true;
            }

            value = string.Empty;
            return false;
        }
    }
}
