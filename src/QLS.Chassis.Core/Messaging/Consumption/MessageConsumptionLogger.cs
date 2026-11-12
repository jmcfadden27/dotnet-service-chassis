using Newtonsoft.Json;
using QLS.Chassis.Core.Logging;
using QLS.Chassis.Core.Messaging.Validation;

namespace QLS.Chassis.Core.Messaging.Consumption;

/// <summary>
/// Default <see cref="IMessageConsumptionLogger"/>. Serializes each event to JSON with the message's header values as
/// metadata. Failures from <see cref="MessageHeaderValidationException"/>, a JSON error or an
/// <see cref="InvalidOperationException"/> are logged with status 400, anything else with 500.
/// </summary>
public sealed class MessageConsumptionLogger : IMessageConsumptionLogger
{
    private readonly IStructuredLogEventFactory _logEventFactory;

    /// <summary>
    /// Creates the logger.
    /// </summary>
    /// <param name="logEventFactory">Builds the structured log events.</param>
    public MessageConsumptionLogger(IStructuredLogEventFactory logEventFactory)
    {
        _logEventFactory = logEventFactory;
    }

    /// <inheritdoc />
    public void LogSucceeded(IMessageExecutionContext executionContext, string operationName, MessageProcessingContext context, long durationMs)
    {
        var metadata = BuildMetadata(context, context.TransportMessageId ?? context.MessageId);

        var evt = _logEventFactory.CreateOperationSucceeded(
            executionContext.ServiceName,
            executionContext.RequestId,
            operationName,
            context.CorrelationId,
            context.TraceId,
            userId: null,
            statusCode: 200,
            durationMs,
            metadata,
            context.MessageId,
            context.CausationId);

        executionContext.LogInformation(JsonConvert.SerializeObject(evt));
    }

    /// <inheritdoc />
    public void LogFailed(IMessageExecutionContext executionContext, string operationName, MessageProcessingContext? context, string transportMessageId, Exception exception, long durationMs)
    {
        var statusCode = IsValidationOrEnvelopeError(exception) ? 400 : 500;
        var metadata = BuildMetadata(context, transportMessageId);

        var evt = _logEventFactory.CreateOperationFailed(
            executionContext.ServiceName,
            executionContext.RequestId,
            operationName,
            context?.CorrelationId,
            context?.TraceId,
            userId: null,
            statusCode,
            durationMs,
            metadata,
            context?.MessageId,
            context?.CausationId,
            exception);

        executionContext.LogError(JsonConvert.SerializeObject(evt));
    }

    private static bool IsValidationOrEnvelopeError(Exception exception)
    {
        return exception is MessageHeaderValidationException
               || exception is JsonException
               || exception is InvalidOperationException;
    }

    private static IReadOnlyDictionary<string, string?> BuildMetadata(MessageProcessingContext? context, string transportMessageId)
    {
        var metadata = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["transportMessageId"] = transportMessageId,
        };

        if (context == null)
            return metadata;

        metadata["messageType"] = context.MessageType;
        metadata["producer"] = context.Producer;
        metadata["schemaVersion"] = context.SchemaVersion.ToString();
        metadata["occurredUtc"] = context.OccurredUtc.ToString("O");
        metadata["messageId"] = context.MessageId;
        metadata["correlationId"] = context.CorrelationId;
        metadata["traceId"] = context.TraceId;
        metadata["causationId"] = context.CausationId;
        return metadata;
    }
}
