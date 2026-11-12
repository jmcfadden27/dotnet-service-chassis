namespace QLS.Chassis.Core.Messaging.Contracts;

/// <summary>
/// A message as it travels: a <see cref="MessageHeader"/> for tracing and de-duplication, and the body.
/// </summary>
/// <typeparam name="T">The body type.</typeparam>
public sealed class MessageEnvelope<T>
{
    /// <summary>
    /// The message metadata. Defaults to an empty header.
    /// </summary>
    public MessageHeader Header { get; set; } = new();
    /// <summary>
    /// The message payload.
    /// </summary>
    public T Body { get; set; } = default!;
}
