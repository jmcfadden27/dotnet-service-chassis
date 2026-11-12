using QLS.Chassis.Core.Configuration;

namespace QLS.Chassis.Core.Tests.Configuration;

public sealed class ConfigurationExceptionTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var inner = new InvalidOperationException("inner");

        var ex = new ConfigurationException("KEY", "bad config", inner);

        Assert.Equal("KEY", ex.Key);
        Assert.Equal("bad config", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }
}
