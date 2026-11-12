namespace QLS.Chassis.Core.Logging;

/// <summary>
/// Makes request data safe to log by redacting secrets, truncating long values and leaving out binary bodies.
/// </summary>
public interface ILogSanitizer
{
    /// <summary>
    /// Sanitizes a request's headers, query string and body.
    /// </summary>
    /// <param name="request">The request data to sanitize.</param>
    /// <returns>The sanitized data, with flags saying whether anything was redacted or truncated.</returns>
    SanitizedRequestLogData Sanitize(LogSanitizerRequest request);
}
