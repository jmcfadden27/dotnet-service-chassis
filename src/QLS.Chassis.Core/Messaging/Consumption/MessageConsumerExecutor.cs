using QLS.Chassis.Core.Messaging.Contracts;
using QLS.Chassis.Core.Idempotency;

namespace QLS.Chassis.Core.Messaging.Consumption;

/// <summary>
/// Default <see cref="IMessageConsumerExecutor"/>. With an idempotency coordinator, each handler runs inside it, keyed by
/// the envelope's message ID (or the transport ID when that is blank) and recorded with status 200.
/// </summary>
public sealed class MessageConsumerExecutor : IMessageConsumerExecutor
{
    private readonly IMessageEnvelopeProcessor _messageEnvelopeProcessor;
    private readonly IMessageConsumptionLogger _messageConsumptionLogger;
    private readonly IMessageIdempotencyCoordinator? _messageIdempotencyCoordinator;

    /// <summary>
    /// Creates the executor.
    /// </summary>
    /// <param name="messageEnvelopeProcessor">Parses and validates each record's envelope.</param>
    /// <param name="messageConsumptionLogger">Logs each record's outcome.</param>
    /// <param name="messageIdempotencyCoordinator">Optional coordinator that stops a redelivered message being handled twice.</param>
    public MessageConsumerExecutor(
        IMessageEnvelopeProcessor messageEnvelopeProcessor,
        IMessageConsumptionLogger messageConsumptionLogger,
        IMessageIdempotencyCoordinator? messageIdempotencyCoordinator = null)
    {
        _messageEnvelopeProcessor = messageEnvelopeProcessor;
        _messageConsumptionLogger = messageConsumptionLogger;
        _messageIdempotencyCoordinator = messageIdempotencyCoordinator;
    }

    /// <inheritdoc />
    public async Task<MessageConsumerExecutionResult> ExecuteAsync<TTransportRecord, TMessage>(
        IEnumerable<TTransportRecord> records,
        IMessageExecutionContext executionContext,
        string operationName,
        Func<TTransportRecord, string> getTransportMessageId,
        Func<TTransportRecord, string> getBody,
        Func<MessageEnvelope<TMessage>, MessageProcessingContext, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default)
    {
        var failedMessageIds = new List<string>();

        foreach (var record in records)
        {
            var transportMessageId = getTransportMessageId(record);
            var body = getBody(record);
            var started = DateTime.UtcNow;
            MessageProcessingContext? processingContext = null;

            try
            {
                var processed = await _messageEnvelopeProcessor.ProcessAsync<TMessage>(
                    body,
                    transportMessageId,
                    cancellationToken);

                processingContext = processed.Context;

                if (_messageIdempotencyCoordinator == null)
                {
                    await handler(processed.Envelope, processingContext, cancellationToken);
                }
                else
                {
                    await _messageIdempotencyCoordinator.ExecuteAsync(
                        BuildIdempotencyRequest(executionContext, operationName, processingContext, body),
                        async () =>
                        {
                            await handler(processed.Envelope, processingContext, cancellationToken);
                            return new MessageIdempotencyExecutionResponse { StatusCode = 200, Body = "processed" };
                        });
                }

                var durationMs = (long)(DateTime.UtcNow - started).TotalMilliseconds;
                _messageConsumptionLogger.LogSucceeded(executionContext, operationName, processingContext, durationMs);
            }
            catch (Exception ex)
            {
                var durationMs = (long)(DateTime.UtcNow - started).TotalMilliseconds;
                _messageConsumptionLogger.LogFailed(executionContext, operationName, processingContext, transportMessageId, ex, durationMs);
                failedMessageIds.Add(transportMessageId);
            }
        }

        return new MessageConsumerExecutionResult
        {
            FailedMessageIds = failedMessageIds,
        };
    }

    private static MessageIdempotencyExecutionRequest BuildIdempotencyRequest(
        IMessageExecutionContext executionContext,
        string operationName,
        MessageProcessingContext context,
        string rawBody)
    {
        var idempotencyKey = string.IsNullOrWhiteSpace(context.MessageId)
            ? context.TransportMessageId ?? string.Empty
            : context.MessageId;

        return new MessageIdempotencyExecutionRequest
        {
            OperationName = operationName,
            ConsumerName = executionContext.ServiceName,
            IdempotencyKey = idempotencyKey,
            MessageBody = rawBody,
        };
    }
}
