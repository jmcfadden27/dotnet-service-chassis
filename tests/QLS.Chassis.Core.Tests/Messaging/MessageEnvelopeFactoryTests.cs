using QLS.Chassis.Core.Messaging.Factory;

namespace QLS.Chassis.Core.Tests.Messaging;

public sealed class MessageEnvelopeFactoryTests
{
    [Fact]
    public void CreateRoot_SetsCoreHeaderFields_WithoutCausation()
    {
        var factory = new MessageEnvelopeFactory();

        var envelope = factory.CreateRoot(new SampleBody { Name = "alpha" }, "order.created", "order-service", "corr-1", "trace-1");

        Assert.Equal(1, envelope.Header.SchemaVersion);
        Assert.Equal("order.created", envelope.Header.MessageType);
        Assert.Equal("order-service", envelope.Header.Producer);
        Assert.Equal("corr-1", envelope.Header.CorrelationId);
        Assert.Equal("trace-1", envelope.Header.TraceId);
        Assert.True(string.IsNullOrWhiteSpace(envelope.Header.CausationId));
        Assert.False(string.IsNullOrWhiteSpace(envelope.Header.MessageId));
        Assert.Equal("alpha", envelope.Body.Name);
    }

    [Fact]
    public void CreateDerived_SetsCausationId()
    {
        var factory = new MessageEnvelopeFactory();

        var envelope = factory.CreateDerived(new SampleBody { Name = "beta" }, "order.updated", "order-service", "corr-2", "trace-2", "msg-1");

        Assert.Equal("msg-1", envelope.Header.CausationId);
        Assert.Equal("beta", envelope.Body.Name);
    }

    private sealed class SampleBody
    {
        public string Name { get; set; } = string.Empty;
    }
}
