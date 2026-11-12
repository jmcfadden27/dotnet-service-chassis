namespace QLS.Chassis.Core.Idempotency;

/// <summary>
/// Default <see cref="IHttpIdempotencyPolicy"/>: reads the <c>Idempotency-Key</c> header, applies to <c>POST</c>,
/// <c>PUT</c>, <c>PATCH</c> and <c>DELETE</c>, keeps records for 24 hours and stores responses with a status code below 500.
/// </summary>
public sealed class DefaultHttpIdempotencyPolicy : IHttpIdempotencyPolicy
{
    /// <inheritdoc />
    public string HeaderName => "Idempotency-Key";
    /// <inheritdoc />
    public TimeSpan RecordTtl => TimeSpan.FromHours(24);

    /// <inheritdoc />
    public bool RequiresForMethod(string? httpMethod)
    {
        return string.Equals(httpMethod, "POST", StringComparison.OrdinalIgnoreCase)
            || string.Equals(httpMethod, "PUT", StringComparison.OrdinalIgnoreCase)
            || string.Equals(httpMethod, "PATCH", StringComparison.OrdinalIgnoreCase)
            || string.Equals(httpMethod, "DELETE", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public bool ShouldPersistResponse(int statusCode)
    {
        return statusCode < 500;
    }
}
