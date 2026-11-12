using QLS.Chassis.Core.Configuration;

namespace QLS.Chassis.Core.Tests.Configuration;

[Collection("EnvironmentVariables")]
public sealed class EnvironmentProviderTests
{
    [Fact]
    public void Get_ReturnsEnvironmentValue()
    {
        const string key = "CHASSIS_CORE_ENV_GET";
        Environment.SetEnvironmentVariable(key, "abc");
        try
        {
            var provider = new EnvironmentProvider();
            Assert.Equal("abc", provider.Get(key));
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, null);
        }
    }

    [Fact]
    public void TryGet_ReturnsFalse_ForMissingOrWhitespace()
    {
        const string missing = "CHASSIS_CORE_ENV_MISSING";
        const string blank = "CHASSIS_CORE_ENV_BLANK";
        Environment.SetEnvironmentVariable(missing, null);
        Environment.SetEnvironmentVariable(blank, "   ");
        try
        {
            var provider = new EnvironmentProvider();
            Assert.False(provider.TryGet(missing, out _));
            Assert.False(provider.TryGet(blank, out _));
        }
        finally
        {
            Environment.SetEnvironmentVariable(blank, null);
        }
    }

    [Fact]
    public void TryGet_ReturnsTrue_ForPopulatedValue()
    {
        const string key = "CHASSIS_CORE_ENV_TRYGET";
        Environment.SetEnvironmentVariable(key, "xyz");
        try
        {
            var provider = new EnvironmentProvider();
            Assert.True(provider.TryGet(key, out var value));
            Assert.Equal("xyz", value);
        }
        finally
        {
            Environment.SetEnvironmentVariable(key, null);
        }
    }
}
