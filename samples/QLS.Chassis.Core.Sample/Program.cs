// A tour of QLS.Chassis.Core: one demo per component, each in its own file under Demos/.
// Run it with: dotnet run --project samples/QLS.Chassis.Core.Sample
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QLS.Chassis.Core.Configuration;
using QLS.Chassis.Core.Extensions;
using QLS.Chassis.Core.Idempotency;
using QLS.Chassis.Core.Logging;
using QLS.Chassis.Core.Messaging.Consumption;
using QLS.Chassis.Core.Messaging.Propagation;
using QLS.Chassis.Core.Resilience;
using QLS.Chassis.Core.Sample;
using QLS.Chassis.Core.Sample.Demos;

var services = new ServiceCollection();
services.AddLogging(logging => logging.AddSimpleConsole(options => options.SingleLine = true));

// Configuration (Demos/ConfigurationDemo.cs)
services.AddSingleton(ConfigurationSourcesBuilder.Build());
services.AddSingleton<IEnvironmentProvider, EnvironmentProvider>();
services.AddSingleton<IConfigReader, ConfigReader>();

// Structured logging (Demos/LoggingDemo.cs)
services.AddSingleton<IStructuredLogEventFactory, DefaultStructuredLogEventFactory>();
services.AddSingleton<ILogSanitizer, DefaultLogSanitizer>();

// Messaging and message idempotency (Demos/MessagingDemo.cs)
services.AddChassisCoreMessaging();
services.AddSingleton<IMessageEnvelopeProcessor, MessageEnvelopeProcessor>();
services.AddSingleton<IMessageConsumptionLogger, MessageConsumptionLogger>();
services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();
services.AddSingleton<IMessageIdempotencyPolicy, DefaultMessageIdempotencyPolicy>();
services.AddSingleton<IMessageIdempotencyCoordinator, MessageIdempotencyCoordinator>();
services.AddSingleton<IMessageConsumerExecutor, MessageConsumerExecutor>();

// Resilience and trace propagation (Demos/ResilienceDemo.cs, Demos/PropagationDemo.cs)
services.AddSingleton<ICorrelationIdAccessor>(new FixedCorrelationIdAccessor("corr-sample-1"));
services.AddSingleton(new ResilienceLoggingOptions { ServiceName = "sample-service" });
services.AddSingleton<IResilienceExecutor, ResilienceExecutor>();
services.AddSingleton<ITraceHeaderAccessor, InboundTraceHeaders>();
services.AddSingleton<IOutboundHeaderPropagator, OutboundHeaderPropagator>();

using var provider = services.BuildServiceProvider();

ConfigurationDemo.Run(provider.GetRequiredService<IConfigReader>());
LoggingDemo.Run(provider.GetRequiredService<IStructuredLogEventFactory>(), provider.GetRequiredService<ILogSanitizer>());
await MessagingDemo.RunAsync(provider);
await ResilienceDemo.RunAsync(provider.GetRequiredService<IResilienceExecutor>());
PropagationDemo.Run(provider.GetRequiredService<IOutboundHeaderPropagator>());
