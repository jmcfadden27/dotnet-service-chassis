using QLS.Chassis.Core.Messaging.Contracts;
using QLS.Chassis.Core.Messaging.Validation;

namespace QLS.Chassis.Core.Tests.Messaging;

public sealed class MessageHeaderValidatorTests
{
    [Fact]
    public void ValidateForPublish_Passes_WhenRequiredFieldsPresent()
    {
        var validator = new MessageHeaderValidator();

        validator.ValidateForPublish(new MessageHeader
        {
            SchemaVersion = 1,
            OccurredUtc = DateTime.SpecifyKind(new DateTime(2026, 1, 1), DateTimeKind.Utc),
            CorrelationId = "corr-1",
            MessageId = "msg-1",
            MessageType = "order.updated",
            Producer = "order-service",
            TraceId = "trace-1"
        });
    }

    [Fact]
    public void ValidateForPublish_Throws_WhenCorrelationMissing()
    {
        var validator = new MessageHeaderValidator();

        Assert.Throws<MessageHeaderValidationException>(() =>
            validator.ValidateForPublish(new MessageHeader
            {
                SchemaVersion = 1,
                OccurredUtc = DateTime.SpecifyKind(new DateTime(2026, 1, 1), DateTimeKind.Utc),
                MessageId = "msg-1",
                MessageType = "order.updated",
                Producer = "order-service",
                TraceId = "trace-1"
            }));
    }

    [Fact]
    public void ValidateForPublish_Throws_WhenOccurredUtcNotUtc()
    {
        var validator = new MessageHeaderValidator();

        Assert.Throws<MessageHeaderValidationException>(() =>
            validator.ValidateForPublish(new MessageHeader
            {
                SchemaVersion = 1,
                OccurredUtc = DateTime.Now,
                CorrelationId = "corr-1",
                MessageId = "msg-1",
                MessageType = "order.updated",
                Producer = "order-service",
                TraceId = "trace-1"
            }));
    }

    [Fact]
    public void ValidateForDerivedPublish_Throws_WhenCausationIdMissing()
    {
        var validator = new MessageHeaderValidator();

        Assert.Throws<MessageHeaderValidationException>(() =>
            validator.ValidateForDerivedPublish(new MessageHeader
            {
                SchemaVersion = 1,
                OccurredUtc = DateTime.SpecifyKind(new DateTime(2026, 1, 1), DateTimeKind.Utc),
                CorrelationId = "corr-1",
                MessageId = "msg-1",
                MessageType = "order.updated",
                Producer = "order-service",
                TraceId = "trace-1"
            }));
    }

    [Fact]
    public void ValidateForDerivedPublish_Passes_WhenCausationIdPresent()
    {
        var validator = new MessageHeaderValidator();

        validator.ValidateForDerivedPublish(new MessageHeader
        {
            SchemaVersion = 1,
            OccurredUtc = DateTime.SpecifyKind(new DateTime(2026, 1, 1), DateTimeKind.Utc),
            CorrelationId = "corr-1",
            MessageId = "msg-1",
            MessageType = "order.updated",
            Producer = "order-service",
            TraceId = "trace-1",
            CausationId = "msg-parent"
        });
    }
}
