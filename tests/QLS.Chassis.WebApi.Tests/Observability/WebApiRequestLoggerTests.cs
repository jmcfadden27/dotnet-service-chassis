using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using QLS.Chassis.Core.Logging;
using QLS.Chassis.WebApi.Observability;
using QLS.Chassis.WebApi.Tests.TestDoubles;

namespace QLS.Chassis.WebApi.Tests.Observability;

public sealed class WebApiRequestLoggerTests
{
    [Fact]
    public void LogSuccess_WritesInformationLog()
    {
        var sink = new ListLogger<WebApiRequestLogger>();
        var factory = new RecordingStructuredLogEventFactory();
        var logger = CreateLogger(sink, factory, new PassthroughSanitizer());

        var context = CreateContext();
        context.Response.StatusCode = 204;

        logger.LogSuccess(context);

        Assert.True(factory.RequestSucceededCalled);
        Assert.Contains(sink.Events, e => e.Level == LogLevel.Information);
    }

    [Fact]
    public void LogError_SanitizesAndWritesErrorLog()
    {
        var sink = new ListLogger<WebApiRequestLogger>();
        var factory = new RecordingStructuredLogEventFactory();
        var sanitizer = new PassthroughSanitizer();
        var logger = CreateLogger(sink, factory, sanitizer);

        var context = CreateContext();
        context.Items["chassis.request.body"] = "{\"password\":\"secret\"}";
        context.Request.Headers["Authorization"] = "Bearer token";
        context.Request.QueryString = new QueryString("?email=a@example.com");

        logger.LogError(context, new InvalidOperationException("boom"), 500);

        Assert.True(factory.RequestFailedCalled);
        Assert.Contains(sink.Events, e => e.Level == LogLevel.Error);
    }

    private static WebApiRequestLogger CreateLogger(
        ListLogger<WebApiRequestLogger> sink,
        RecordingStructuredLogEventFactory factory,
        ILogSanitizer sanitizer)
    {
        return new WebApiRequestLogger(
            sink,
            new HostEnvironmentStub { ApplicationName = "QLS.Chassis.WebApi" },
            factory,
            sanitizer);
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.TraceIdentifier = "trace-1";
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/users";
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "user-1")], "test"));
        return context;
    }

    private sealed class PassthroughSanitizer : ILogSanitizer
    {
        public SanitizedRequestLogData Sanitize(LogSanitizerRequest request)
        {
            return new SanitizedRequestLogData
            {
                Headers = request.Headers?.ToDictionary(x => x.Key, x => (string?)x.Value) ?? new Dictionary<string, string?>(),
                QueryStringParameters = request.QueryStringParameters?.ToDictionary(x => x.Key, x => (string?)x.Value) ?? new Dictionary<string, string?>(),
                Body = request.Body,
                BodyLogged = true,
                BodyReason = "ok",
                RedactionApplied = false,
                Truncated = false,
            };
        }
    }

    private sealed class RecordingStructuredLogEventFactory : IStructuredLogEventFactory
    {
        public bool RequestSucceededCalled { get; private set; }
        public bool RequestFailedCalled { get; private set; }

        public StructuredLogEvent CreateRequestSucceeded(string service, string requestId, string? correlationId, string? traceId, string? userId, string? httpMethod, string? path, int statusCode)
        {
            RequestSucceededCalled = true;
            return new StructuredLogEvent { EventName = "request_succeeded", Operation = $"{httpMethod} {path}", StatusCode = statusCode };
        }

        public StructuredLogEvent CreateRequestFailed(string service, string requestId, string? correlationId, string? traceId, string? userId, string? httpMethod, string? path, int statusCode, Exception exception, IReadOnlyDictionary<string, object?>? additionalMetadata = null)
        {
            RequestFailedCalled = true;
            return new StructuredLogEvent { EventName = "request_failed", Operation = $"{httpMethod} {path}", StatusCode = statusCode, ErrorMessage = exception.Message };
        }

        public StructuredLogEvent CreateOperationSucceeded(string service, string requestId, string operation, string? correlationId, string? traceId, string? userId, int statusCode, long durationMs, IReadOnlyDictionary<string, string?>? operationMetadata, string? messageId, string? causationId)
            => new();

        public StructuredLogEvent CreateOperationFailed(string service, string requestId, string operation, string? correlationId, string? traceId, string? userId, int statusCode, long durationMs, IReadOnlyDictionary<string, string?>? operationMetadata, string? messageId, string? causationId, Exception exception)
            => new();

        public StructuredLogEvent CreateOperationWarning(string service, string requestId, string operation, string? correlationId, string? traceId, string? userId, int statusCode, long durationMs, IReadOnlyDictionary<string, string?>? operationMetadata, string? messageId, string? causationId, Exception exception)
            => new();
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Events { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Events.Add((logLevel, formatter(state, exception)));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
