using QLS.Chassis.Core.Messaging.Contracts;

namespace QLS.Chassis.Core.Messaging.Consumption;

/// <summary>
/// Handles a batch of transport records as message envelopes, logging each one and reporting which failed.
/// </summary>
public interface IMessageConsumerExecutor
{
    /// <summary>
    /// Processes each record in turn: parses and validates its envelope, runs <paramref name="handler"/> (inside the
    /// <see cref="QLS.Chassis.Core.Idempotency.IMessageIdempotencyCoordinator"/> when one is configured) and logs the outcome.
    /// A failing record is logged and reported; the rest of the batch carries on.
    /// </summary>
    /// <typeparam name="TTransportRecord">The transport's record type, for example an SQS message.</typeparam>
    /// <typeparam name="TMessage">The envelope body type.</typeparam>
    /// <param name="records">The records to handle.</param>
    /// <param name="executionContext">The service name, request ID and log writers for this invocation.</param>
    /// <param name="operationName">The operation name for log events and idempotency records.</param>
    /// <param name="getTransportMessageId">Gets a record's transport message ID.</param>
    /// <param name="getBody">Gets a record's raw body.</param>
    /// <param name="handler">Handles one parsed message.</param>
    /// <param name="cancellationToken">Passed to the envelope processor and the handler.</param>
    /// <returns>The transport message IDs of the records that failed.</returns>
    Task<MessageConsumerExecutionResult> ExecuteAsync<TTransportRecord, TMessage>(
        IEnumerable<TTransportRecord> records,
        IMessageExecutionContext executionContext,
        string operationName,
        Func<TTransportRecord, string> getTransportMessageId,
        Func<TTransportRecord, string> getBody,
        Func<MessageEnvelope<TMessage>, MessageProcessingContext, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default);
}
