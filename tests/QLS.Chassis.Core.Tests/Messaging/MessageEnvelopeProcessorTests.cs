using Newtonsoft.Json;
using QLS.Chassis.Core.Messaging.Consumption;
using QLS.Chassis.Core.Messaging.Contracts;
using QLS.Chassis.Core.Messaging.Validation;

namespace QLS.Chassis.Core.Tests.Messaging;

public sealed class MessageEnvelopeProcessorTests
{
    [Fact]
    public async Task ProcessAsync_ParsesEnvelopeAndBuildsContext()
    {
        var processor = new MessageEnvelopeProcessor(new MessageHeaderValidator());
        var envelope = new MessageEnvelope<SampleBody>
        {
            Header = new MessageHeader
            {
                SchemaVersion = 1,
                OccurredUtc = DateTime.SpecifyKind(new DateTime(2026, 2, 1, 10, 11, 12), DateTimeKind.Utc),
                CorrelationId = "corr-1",
                MessageId = "msg-1",
                MessageType = "sample.created",
                Producer = "sample-service",
                CausationId = "cause-1",
                TraceId = "trace-1"
            },
            Body = new SampleBody { Name = "alpha" }
        };

        var json = JsonConvert.SerializeObject(envelope);
        var result = await processor.ProcessAsync<SampleBody>(json, "sqs-1");

        Assert.Equal("alpha", result.Envelope.Body.Name);
        Assert.Equal("corr-1", result.Context.CorrelationId);
        Assert.Equal("msg-1", result.Context.MessageId);
        Assert.Equal("sample.created", result.Context.MessageType);
        Assert.Equal("sample-service", result.Context.Producer);
        Assert.Equal("sqs-1", result.Context.TransportMessageId);
    }

    [Fact]
    public async Task ProcessAsync_Throws_WhenHeaderInvalid()
    {
        var processor = new MessageEnvelopeProcessor(new MessageHeaderValidator());
        var envelope = new MessageEnvelope<SampleBody>
        {
            Header = new MessageHeader
            {
                SchemaVersion = 1,
                OccurredUtc = DateTime.SpecifyKind(new DateTime(2026, 2, 1, 10, 11, 12), DateTimeKind.Utc),
                CorrelationId = "corr-1",
                MessageId = "msg-1",
                MessageType = "sample.created",
                Producer = "sample-service"
            },
            Body = new SampleBody { Name = "alpha" }
        };

        var json = JsonConvert.SerializeObject(envelope);

        await Assert.ThrowsAsync<MessageHeaderValidationException>(() =>
            processor.ProcessAsync<SampleBody>(json, "sqs-1"));
    }

    private sealed class SampleBody
    {
        public string Name { get; set; } = string.Empty;
    }
}
