namespace QLS.Chassis.Core.Messaging.Consumption;

/// <summary>
/// Writes one structured log event per consumed message.
/// </summary>
public interface IMessageConsumptionLogger
{
    /// <summary>
    /// Logs an <c>operation_succeeded</c> event for a handled message.
    /// </summary>
    /// <param name="executionContext">The service name, request ID and log writers.</param>
    /// <param name="operationName">The operation name.</param>
    /// <param name="context">The message's header values.</param>
    /// <param name="durationMs">How long handling took, in milliseconds.</param>
    void LogSucceeded(IMessageExecutionContext executionContext, string operationName, MessageProcessingContext context, long durationMs);
    /// <summary>
    /// Logs an <c>operation_failed</c> event for a message that failed.
    /// </summary>
    /// <param name="executionContext">The service name, request ID and log writers.</param>
    /// <param name="operationName">The operation name.</param>
    /// <param name="context">The message's header values, or <see langword="null"/> when the envelope wasn't parsed.</param>
    /// <param name="transportMessageId">The transport's message ID.</param>
    /// <param name="exception">The failure.</param>
    /// <param name="durationMs">How long handling took, in milliseconds.</param>
    void LogFailed(IMessageExecutionContext executionContext, string operationName, MessageProcessingContext? context, string transportMessageId, Exception exception, long durationMs);
}
