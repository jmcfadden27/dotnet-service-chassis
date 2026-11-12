using Moq;
using QLS.Chassis.Core.Messaging.Consumption;
using QLS.Chassis.Core.Messaging.Contracts;

namespace QLS.Chassis.Core.Tests.Messaging;

public sealed class MessageConsumerExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_UsesCoreFlow_AndReturnsFailedIds()
    {
        var logger = new Mock<IMessageConsumptionLogger>(MockBehavior.Strict);
        var processor = new StubProcessor();
        var executor = new MessageConsumerExecutor(processor, logger.Object);

        var records = new[]
        {
            new TestTransportRecord { MessageId = "ok-1", Body = "ok" },
            new TestTransportRecord { MessageId = "bad-1", Body = "bad" }
        };

        var executionContext = new MessageExecutionContext
        {
            ServiceName = "sample-service",
            RequestId = "req-1",
            LogInfo = _ => { },
            LogErr = _ => { }
        };

        logger.Setup(l => l.LogSucceeded(
            It.IsAny<IMessageExecutionContext>(),
            "sqs.sample.consume",
            It.Is<MessageProcessingContext>(c => c.MessageId == "msg-ok" && c.TransportMessageId == "ok-1"),
            It.IsAny<long>()));

        logger.Setup(l => l.LogFailed(
            It.IsAny<IMessageExecutionContext>(),
            "sqs.sample.consume",
            null,
            "bad-1",
            It.IsAny<InvalidOperationException>(),
            It.IsAny<long>()));

        var handled = 0;
        var result = await executor.ExecuteAsync<TestTransportRecord, SampleBody>(
            records,
            executionContext,
            "sqs.sample.consume",
            record => record.MessageId,
            record => record.Body,
            (envelope, _, _) =>
            {
                handled++;
                Assert.Equal("alpha", envelope.Body.Name);
                return Task.CompletedTask;
            });

        Assert.Equal(1, handled);
        Assert.Single(result.FailedMessageIds);
        Assert.Equal("bad-1", result.FailedMessageIds[0]);
        logger.VerifyAll();
    }

    private sealed class StubProcessor : IMessageEnvelopeProcessor
    {
        public Task<MessageEnvelopeProcessingResult<TMessage>> ProcessAsync<TMessage>(string rawMessageBody, string? transportMessageId = null, CancellationToken cancellationToken = default)
        {
            if (string.Equals(rawMessageBody, "bad", StringComparison.Ordinal))
                throw new InvalidOperationException("bad message");

            var envelope = new MessageEnvelope<SampleBody>
            {
                Header = new MessageHeader
                {
                    SchemaVersion = 1,
                    OccurredUtc = DateTime.UtcNow,
                    CorrelationId = "corr-1",
                    MessageId = "msg-ok",
                    MessageType = "sample.created",
                    Producer = "sample-service",
                    TraceId = "trace-1"
                },
                Body = new SampleBody { Name = "alpha" }
            };

            var result = new MessageEnvelopeProcessingResult<SampleBody>
            {
                Envelope = envelope,
                Context = new MessageProcessingContext
                {
                    CorrelationId = envelope.Header.CorrelationId!,
                    TraceId = envelope.Header.TraceId!,
                    MessageId = envelope.Header.MessageId!,
                    MessageType = envelope.Header.MessageType!,
                    Producer = envelope.Header.Producer!,
                    SchemaVersion = envelope.Header.SchemaVersion,
                    OccurredUtc = envelope.Header.OccurredUtc,
                    TransportMessageId = transportMessageId
                }
            };

            return Task.FromResult((MessageEnvelopeProcessingResult<TMessage>)(object)result);
        }
    }

    private sealed class SampleBody
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestTransportRecord
    {
        public string MessageId { get; init; } = string.Empty;
        public string Body { get; init; } = string.Empty;
    }
}
