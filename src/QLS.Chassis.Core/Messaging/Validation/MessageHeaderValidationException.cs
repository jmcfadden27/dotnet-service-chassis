namespace QLS.Chassis.Core.Messaging.Validation;

/// <summary>
/// Thrown when a message header is missing a required field or has an invalid one.
/// </summary>
public sealed class MessageHeaderValidationException : Exception
{
    /// <summary>
    /// Creates the exception.
    /// </summary>
    /// <param name="message">The error message, naming the field.</param>
    public MessageHeaderValidationException(string message)
        : base(message)
    {
    }
}
