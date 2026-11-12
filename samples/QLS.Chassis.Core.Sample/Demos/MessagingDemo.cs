using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using QLS.Chassis.Core.Messaging.Consumption;
using QLS.Chassis.Core.Messaging.Factory;

namespace QLS.Chassis.Core.Sample.Demos;

/// <summary>
/// Publish a message in an envelope, then consume a batch: the same message twice (handled once, thanks to message
/// idempotency) and one that isn't a valid envelope (reported as failed, without stopping the batch).
/// </summary>
internal static class MessagingDemo
{
    public static async Task RunAsync(IServiceProvider provider)
    {
        Section.Title("Messaging");

        // Publish: a validated envelope, serialized as you would before sending it to a queue.
        var envelopes = provider.GetRequiredService<IValidatedMessageEnvelopeFactory>();
        var envelope = envelopes.CreateRoot(new OrderCreated("o-1", 42.50m), "order.created", "order-service", "corr-sample-1", "trace-sample-1");
        var raw = JsonConvert.SerializeObject(envelope);
        Console.WriteLine($"Published {envelope.Header.MessageType} with message ID {envelope.Header.MessageId}");

        // A message sent while handling another keeps the correlation ID and points back at its cause.
        var derived = envelopes.CreateDerived(new InvoiceRequested("o-1"), "invoice.requested", "invoice-service",
            envelope.Header.CorrelationId!, envelope.Header.TraceId!, causationId: envelope.Header.MessageId!);
        Console.WriteLine($"Derived {derived.Header.MessageType}: correlation {derived.Header.CorrelationId}, caused by {derived.Header.CausationId}");

        // Consume a batch of transport records (here simple tuples; in Lambda, SQS messages).
        var handled = 0;
        var executor = provider.GetRequiredService<IMessageConsumerExecutor>();
        var context = new MessageExecutionContext
        {
            ServiceName = "invoice-service",
            RequestId = "req-sample-1",
            LogInfo = message => Console.WriteLine($"  log: {Shorten(message)}"),
            LogErr = message => Console.WriteLine($"  log: {Shorten(message)}"),
        };
        var batch = new[] { ("sqs-1", raw), ("sqs-2", raw), ("sqs-3", "{not-json") };

        var result = await executor.ExecuteAsync<(string Id, string Body), OrderCreated>(
            batch,
            context,
            operationName: "invoices.create-for-order",
            getTransportMessageId: record => record.Id,
            getBody: record => record.Body,
            handler: (message, _, _) =>
            {
                handled++;
                Console.WriteLine($"Handling order {message.Body.OrderId} for {message.Body.Total:C}");
                return Task.CompletedTask;
            });

        Console.WriteLine($"Handler ran {handled} time(s) for 2 deliveries; failed transport IDs: {string.Join(", ", result.FailedMessageIds)}");
    }

    private static string Shorten(string json) => json.Length <= 140 ? json : json[..140] + "…";
}
