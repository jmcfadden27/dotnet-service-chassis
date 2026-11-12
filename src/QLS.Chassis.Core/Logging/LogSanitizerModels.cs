namespace QLS.Chassis.Core.Logging;

/// <summary>
/// The request data passed to <see cref="ILogSanitizer.Sanitize"/>.
/// </summary>
public sealed class LogSanitizerRequest
{
    /// <summary>
    /// The request headers. <c>Content-Type</c> decides how the body is handled.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Headers { get; init; }
    /// <summary>
    /// The query string parameters.
    /// </summary>
    public IReadOnlyDictionary<string, string>? QueryStringParameters { get; init; }
    /// <summary>
    /// The request body.
    /// </summary>
    public string? Body { get; init; }
    /// <summary>
    /// Whether the body is base64-encoded, in which case it is left out.
    /// </summary>
    public bool IsBase64Encoded { get; init; }
}

/// <summary>
/// Request data that is safe to log.
/// </summary>
public sealed class SanitizedRequestLogData
{
    /// <summary>
    /// The headers, with sensitive values redacted and long values truncated.
    /// </summary>
    public IReadOnlyDictionary<string, string?> Headers { get; init; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// The query string parameters, with sensitive values redacted and long values truncated.
    /// </summary>
    public IReadOnlyDictionary<string, string?> QueryStringParameters { get; init; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// The sanitized body, or <see langword="null"/> when it was left out.
    /// </summary>
    public string? Body { get; init; }
    /// <summary>
    /// Whether the body was logged.
    /// </summary>
    public bool BodyLogged { get; init; }
    /// <summary>
    /// Why the body was left out (<c>binary_or_base64</c>), or <see langword="null"/> when it was logged.
    /// </summary>
    public string? BodyReason { get; init; }
    /// <summary>
    /// Whether any value was redacted.
    /// </summary>
    public bool RedactionApplied { get; init; }
    /// <summary>
    /// Whether any value or the body was truncated.
    /// </summary>
    public bool Truncated { get; init; }
}
