using QLS.Chassis.Core.Resilience;

namespace QLS.Chassis.Core.Tests.Resilience;

public sealed class ResilienceOptionsAndExceptionTests
{
    [Fact]
    public void ResilienceOptions_HasExpectedDefaults()
    {
        var options = new ResilienceOptions { DependencyName = "dep" };

        Assert.Equal("dep", options.DependencyName);
        Assert.Equal(2500, options.TimeoutMs);
        Assert.Equal(2, options.MaxRetries);
        Assert.Equal(200, options.BaseDelayMs);
        Assert.Equal(0.5, options.CircuitFailureRatio);
        Assert.Equal(10, options.CircuitMinThroughput);
        Assert.Equal(30, options.CircuitBreakSeconds);
        Assert.Equal(30, options.RetryAfterSeconds);
    }

    [Fact]
    public void ResilienceExecutionException_SetsProperties()
    {
        var inner = new HttpRequestException("boom");
        var ex = new ResilienceExecutionException("billing", ResilienceFailureType.Timeout, "timed out", inner, 20);

        Assert.Equal("billing", ex.DependencyName);
        Assert.Equal(ResilienceFailureType.Timeout, ex.FailureType);
        Assert.Equal("timed out", ex.Message);
        Assert.Same(inner, ex.InnerException);
        Assert.Equal(20, ex.RetryAfterSeconds);
    }
}
