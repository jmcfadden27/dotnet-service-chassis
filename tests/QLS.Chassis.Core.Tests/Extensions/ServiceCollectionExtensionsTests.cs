using Microsoft.Extensions.DependencyInjection;
using QLS.Chassis.Core.Extensions;
using QLS.Chassis.Core.Messaging.Factory;
using QLS.Chassis.Core.Messaging.Validation;

namespace QLS.Chassis.Core.Tests.Extensions;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddChassisCoreMessaging_RegistersMessagingServices()
    {
        var services = new ServiceCollection();

        services.AddChassisCoreMessaging();

        Assert.Contains(services, d => d.ServiceType == typeof(IMessageEnvelopeFactory)
                                       && d.ImplementationType == typeof(MessageEnvelopeFactory)
                                       && d.Lifetime == ServiceLifetime.Singleton);

        Assert.Contains(services, d => d.ServiceType == typeof(IMessageHeaderValidator)
                                       && d.ImplementationType == typeof(MessageHeaderValidator)
                                       && d.Lifetime == ServiceLifetime.Singleton);

        Assert.Contains(services, d => d.ServiceType == typeof(IValidatedMessageEnvelopeFactory)
                                       && d.ImplementationType == typeof(ValidatedMessageEnvelopeFactory)
                                       && d.Lifetime == ServiceLifetime.Singleton);
    }
}
