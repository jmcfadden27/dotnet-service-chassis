using Moq;
using QLS.Chassis.Core.Configuration;
using QLS.Chassis.Core.Messaging.Headers;
using QLS.Chassis.Core.Messaging.Propagation;
using QLS.Chassis.Core.Resilience;

namespace QLS.Chassis.Core.Tests.Messaging;

public sealed class OutboundHeaderPropagatorTests
{
    [Fact]
    public void Apply_PropagatesCorrelationAndBothTraceHeaders_FromXRayWhenAvailable()
    {
        var correlation = new Mock<ICorrelationIdAccessor>();
        correlation.SetupGet(c => c.CorrelationId).Returns("corr-1");

        var environment = new Mock<IEnvironmentProvider>();
        environment.Setup(e => e.Get("_X_AMZN_TRACE_ID"))
            .Returns("Root=1-65f9f9f9-1234567890abcdef12345678;Parent=abcdef1234567890;Sampled=1");

        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var propagator = new OutboundHeaderPropagator(correlation.Object, environment.Object);

        propagator.Apply(request);

        Assert.True(request.Headers.TryGetValues(ChassisHeaderNames.CorrelationId, out var correlationValues));
        Assert.Equal("corr-1", correlationValues.Single());
        Assert.True(request.Headers.TryGetValues(ChassisHeaderNames.XAmznTraceId, out var xrayValues));
        Assert.Equal("Root=1-65f9f9f9-1234567890abcdef12345678;Parent=abcdef1234567890;Sampled=1", xrayValues.Single());
        Assert.True(request.Headers.TryGetValues(ChassisHeaderNames.TraceParent, out var traceParentValues));
        Assert.Equal("00-65f9f9f91234567890abcdef12345678-abcdef1234567890-01", traceParentValues.Single());
    }

    [Fact]
    public void Apply_GeneratesSyntheticTraceHeaders_WhenXRayMissing()
    {
        var correlation = new Mock<ICorrelationIdAccessor>();
        correlation.SetupGet(c => c.CorrelationId).Returns("corr-2");

        var environment = new Mock<IEnvironmentProvider>();
        environment.Setup(e => e.Get("_X_AMZN_TRACE_ID")).Returns((string?)null);

        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var propagator = new OutboundHeaderPropagator(correlation.Object, environment.Object);

        propagator.Apply(request);

        var xray = request.Headers.GetValues(ChassisHeaderNames.XAmznTraceId).Single();
        var traceParent = request.Headers.GetValues(ChassisHeaderNames.TraceParent).Single();

        Assert.StartsWith("Root=1-", xray);
        Assert.StartsWith("00-", traceParent);
    }

    [Fact]
    public void Apply_UsesInboundTraceParent_WhenProvidedByAccessor()
    {
        var correlation = new Mock<ICorrelationIdAccessor>();
        correlation.SetupGet(c => c.CorrelationId).Returns("corr-3");

        var environment = new Mock<IEnvironmentProvider>();
        var traceAccessor = new Mock<ITraceHeaderAccessor>();
        traceAccessor.SetupGet(t => t.TraceParent)
            .Returns("00-65f9f9f91234567890abcdef12345678-abcdef1234567890-01");
        traceAccessor.SetupGet(t => t.XAmznTraceId).Returns((string?)null);

        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        var propagator = new OutboundHeaderPropagator(correlation.Object, environment.Object, traceAccessor.Object);

        propagator.Apply(request);

        Assert.Equal("00-65f9f9f91234567890abcdef12345678-abcdef1234567890-01", request.Headers.GetValues(ChassisHeaderNames.TraceParent).Single());
        Assert.Equal("Root=1-65f9f9f9-1234567890abcdef12345678;Parent=abcdef1234567890;Sampled=1", request.Headers.GetValues(ChassisHeaderNames.XAmznTraceId).Single());
    }

    [Fact]
    public void Apply_Throws_WhenCorrelationMissing()
    {
        var correlation = new Mock<ICorrelationIdAccessor>();
        correlation.SetupGet(c => c.CorrelationId).Returns(" ");

        var environment = new Mock<IEnvironmentProvider>();
        var propagator = new OutboundHeaderPropagator(correlation.Object, environment.Object);

        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        Assert.Throws<OutboundHeaderPropagationException>(() => propagator.Apply(request));
    }
}
