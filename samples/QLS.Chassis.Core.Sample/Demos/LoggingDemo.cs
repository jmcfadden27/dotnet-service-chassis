using Newtonsoft.Json;
using QLS.Chassis.Core.Logging;

namespace QLS.Chassis.Core.Sample.Demos;

/// <summary>One structured event shape for every service, and redacting secrets before a request is logged.</summary>
internal static class LoggingDemo
{
    public static void Run(IStructuredLogEventFactory events, ILogSanitizer sanitizer)
    {
        Section.Title("Structured logging");

        var failure = events.CreateOperationFailed(
            service: "order-service",
            requestId: "req-sample-1",
            operation: "orders.ship",
            correlationId: "corr-sample-1",
            traceId: "trace-sample-1",
            userId: "user-1",
            statusCode: 409,
            durationMs: 42,
            operationMetadata: new Dictionary<string, string?> { ["orderId"] = "o-1" },
            messageId: null,
            causationId: null,
            exception: new InvalidOperationException("Order o-1 has already shipped."));
        Console.WriteLine($"Failure event: {failure.EventName}, category '{failure.ErrorCategory}' (from status 409)");

        var sanitized = sanitizer.Sanitize(new LogSanitizerRequest
        {
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json",
                ["Authorization"] = "Bearer eyJhbGciOi...",
                ["X-Api-Key"] = "live-key-123",
            },
            QueryStringParameters = new Dictionary<string, string> { ["page"] = "2", ["token"] = "abc" },
            Body = "{\"email\":\"a@example.com\",\"password\":\"hunter2\",\"profile\":{\"name\":\"Ada\",\"secretCode\":\"x\"}}",
        });
        Console.WriteLine($"Sanitized headers: {JsonConvert.SerializeObject(sanitized.Headers)}");
        Console.WriteLine($"Sanitized query:   {JsonConvert.SerializeObject(sanitized.QueryStringParameters)}");
        Console.WriteLine($"Sanitized body:    {sanitized.Body}");
        Console.WriteLine($"Redaction applied: {sanitized.RedactionApplied}");
    }
}
