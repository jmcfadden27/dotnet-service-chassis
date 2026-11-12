using QLS.Chassis.Core.Resilience;

namespace QLS.Chassis.Core.Tests.Resilience;

public sealed class ResilienceDefaultsTests
{
    [Fact]
    public void For_UsesProfileDefaults()
    {
        var options = ResilienceDefaults.For("billing-entitlements", new ResilienceDefaultsProfile());

        Assert.Equal("billing-entitlements", options.DependencyName);
        Assert.Equal(2500, options.TimeoutMs);
        Assert.Equal(2, options.MaxRetries);
        Assert.Equal(200, options.BaseDelayMs);
        Assert.Equal(0.5d, options.CircuitFailureRatio);
        Assert.Equal(10, options.CircuitMinThroughput);
        Assert.Equal(30, options.CircuitBreakSeconds);
        Assert.Equal(30, options.RetryAfterSeconds);
    }

    [Fact]
    public void For_UsesCustomProfileOverrides()
    {
        var options = ResilienceDefaults.For("external-api", new CustomResilienceProfile());

        Assert.Equal(6000, options.TimeoutMs);
        Assert.Equal(1, options.MaxRetries);
        Assert.Equal(50, options.BaseDelayMs);
        Assert.Equal(0.8d, options.CircuitFailureRatio);
        Assert.Equal(5, options.CircuitMinThroughput);
        Assert.Equal(15, options.CircuitBreakSeconds);
        Assert.Equal(10, options.RetryAfterSeconds);
    }

    [Fact]
    public void For_NormalizesInvalidProfileValues()
    {
        var options = ResilienceDefaults.For("dep", new InvalidResilienceProfile());

        Assert.Equal(2500, options.TimeoutMs);
        Assert.Equal(2, options.MaxRetries);
        Assert.Equal(200, options.BaseDelayMs);
        Assert.Equal(0.1d, options.CircuitFailureRatio);
        Assert.Equal(10, options.CircuitMinThroughput);
        Assert.Equal(30, options.CircuitBreakSeconds);
        Assert.Equal(30, options.RetryAfterSeconds);
    }

    [Fact]
    public void For_Throws_WhenDependencyNameMissing()
    {
        Assert.Throws<ArgumentException>(() => ResilienceDefaults.For(" ", new ResilienceDefaultsProfile()));
    }

    [Fact]
    public void For_UsesStaticProfileByDefault()
    {
        var previous = ResilienceDefaults.Profile;
        ResilienceDefaults.Profile = new CustomResilienceProfile();
        try
        {
            var options = ResilienceDefaults.For("dep");
            Assert.Equal(6000, options.TimeoutMs);
        }
        finally
        {
            ResilienceDefaults.Profile = previous;
        }
    }

    private sealed class CustomResilienceProfile : ResilienceDefaultsProfile
    {
        public override int TimeoutMs => 6000;
        public override int MaxRetries => 1;
        public override int BaseDelayMs => 50;
        public override double CircuitFailureRatio => 0.8d;
        public override int CircuitMinThroughput => 5;
        public override int CircuitBreakSeconds => 15;
        public override int RetryAfterSeconds => 10;
    }

    private sealed class InvalidResilienceProfile : ResilienceDefaultsProfile
    {
        public override int TimeoutMs => 0;
        public override int MaxRetries => -1;
        public override int BaseDelayMs => 0;
        public override double CircuitFailureRatio => -1d;
        public override int CircuitMinThroughput => 0;
        public override int CircuitBreakSeconds => 0;
        public override int RetryAfterSeconds => 0;
    }
}
