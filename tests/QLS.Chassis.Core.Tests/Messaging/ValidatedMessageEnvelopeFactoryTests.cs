using Moq;
using QLS.Chassis.Core.Messaging.Factory;
using QLS.Chassis.Core.Messaging.Validation;

namespace QLS.Chassis.Core.Tests.Messaging;

public sealed class ValidatedMessageEnvelopeFactoryTests
{
    [Fact]
    public void CreateRoot_UsesPublishValidation()
    {
        var validator = new Mock<IMessageHeaderValidator>();
        var factory = new ValidatedMessageEnvelopeFactory(new MessageEnvelopeFactory(), validator.Object);

        var envelope = factory.CreateRoot(new SampleBody { Name = "alpha" }, "order.created", "order-service", "corr-1", "trace-1");

        Assert.Equal("alpha", envelope.Body.Name);
        validator.Verify(v => v.ValidateForPublish(envelope.Header), Times.Once);
        validator.Verify(v => v.ValidateForDerivedPublish(It.IsAny<QLS.Chassis.Core.Messaging.Contracts.MessageHeader>()), Times.Never);
    }

    [Fact]
    public void CreateDerived_UsesDerivedValidation()
    {
        var validator = new Mock<IMessageHeaderValidator>();
        var factory = new ValidatedMessageEnvelopeFactory(new MessageEnvelopeFactory(), validator.Object);

        var envelope = factory.CreateDerived(new SampleBody { Name = "beta" }, "order.updated", "order-service", "corr-1", "trace-1", "msg-1");

        Assert.Equal("beta", envelope.Body.Name);
        validator.Verify(v => v.ValidateForDerivedPublish(envelope.Header), Times.Once);
        validator.Verify(v => v.ValidateForPublish(It.IsAny<QLS.Chassis.Core.Messaging.Contracts.MessageHeader>()), Times.Never);
    }

    private sealed class SampleBody
    {
        public string Name { get; set; } = string.Empty;
    }
}
