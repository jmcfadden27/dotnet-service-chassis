using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using QLS.Chassis.Core.Logging;
using QLS.Chassis.WebApi.Observability;
using QLS.Chassis.WebApi.Tests.TestDoubles;

namespace QLS.Chassis.WebApi.Tests.Observability;

public sealed class StructuredAppLoggerTests
{
    [Fact]
    public void LogOperationSucceeded_WritesInformationLog()
    {
        var sink = new ListLogger<StructuredAppLogger>();
        var logger = CreateLogger(sink);

        logger.LogOperationSucceeded("test_operation", 200, operationMetadata: new Dictionary<string, string?> { ["k"] = "v" });

        Assert.Contains(sink.Events, e => e.Level == LogLevel.Information && e.Message.Contains("test_operation", StringComparison.Ordinal));
    }

    [Fact]
    public void LogOperationFailed_WritesErrorLog()
    {
        var sink = new ListLogger<StructuredAppLogger>();
        var logger = CreateLogger(sink);

        logger.LogOperationFailed("test_operation", new InvalidOperationException("boom"), 500);

        Assert.Contains(sink.Events, e => e.Level == LogLevel.Error && e.Message.Contains("test_operation", StringComparison.Ordinal));
    }

    [Fact]
    public void LogOperationSucceeded_Throws_WhenOperationMissing()
    {
        var sink = new ListLogger<StructuredAppLogger>();
        var logger = CreateLogger(sink);

        Assert.Throws<ArgumentException>(() => logger.LogOperationSucceeded("", 200));
    }

    private static StructuredAppLogger CreateLogger(ListLogger<StructuredAppLogger> sink)
    {
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "trace-123",
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "user-123")], "test"))
        };

        return new StructuredAppLogger(
            sink,
            new HttpContextAccessor { HttpContext = context },
            new HostEnvironmentStub { ApplicationName = "QLS.Chassis.WebApi" },
            new DefaultStructuredLogEventFactory());
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
