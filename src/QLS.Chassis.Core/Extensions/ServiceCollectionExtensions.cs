using Microsoft.Extensions.DependencyInjection;
using QLS.Chassis.Core.Messaging.Factory;
using QLS.Chassis.Core.Messaging.Validation;

namespace QLS.Chassis.Core.Extensions;

/// <summary>
/// Registers the chassis core services with a <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IMessageEnvelopeFactory"/>, <see cref="IMessageHeaderValidator"/> and
    /// <see cref="IValidatedMessageEnvelopeFactory"/> as singletons. Other components are registered by the caller.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddChassisCoreMessaging(this IServiceCollection services)
    {
        services.AddSingleton<IMessageEnvelopeFactory, MessageEnvelopeFactory>();
        services.AddSingleton<IMessageHeaderValidator, MessageHeaderValidator>();
        services.AddSingleton<IValidatedMessageEnvelopeFactory, ValidatedMessageEnvelopeFactory>();

        return services;
    }
}
