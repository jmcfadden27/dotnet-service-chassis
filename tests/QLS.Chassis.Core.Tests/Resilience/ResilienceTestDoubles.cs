using Microsoft.Extensions.Logging;
using QLS.Chassis.Core.Resilience;

namespace QLS.Chassis.Core.Tests.Resilience;

internal sealed class StubCorrelationIdAccessor : ICorrelationIdAccessor
{
    public string? CorrelationId { get; set; }
}

internal sealed class RecordingLogger<T> : ILogger<T>
{
    private readonly object _gate = new();
    private readonly List<(LogLevel Level, string Message)> _entries = new();

    public LogLevel MinimumLevel { get; init; } = LogLevel.Trace;

    public IReadOnlyList<(LogLevel Level, string Message)> Entries
    {
        get
        {
            lock (_gate)
                return _entries.ToList();
        }
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= MinimumLevel;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        lock (_gate)
            _entries.Add((logLevel, formatter(state, exception)));
    }
}
