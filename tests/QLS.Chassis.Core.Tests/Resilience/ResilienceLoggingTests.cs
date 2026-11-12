using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using QLS.Chassis.Core.Resilience;

namespace QLS.Chassis.Core.Tests.Resilience;

public sealed class ResilienceLoggingTests
{
    [Fact]
    public async Task ExecuteAsync_LogsRetryAndFailure_AsStructuredEvents_ThroughTheLogger()
    {
        var logger = new RecordingLogger<ResilienceExecutor>();
        var executor = new ResilienceExecutor(
            new StubCorrelationIdAccessor { CorrelationId = "corr-1" },
            logger,
            new ResilienceLoggingOptions { ServiceName = "order-service" });
        var options = new ResilienceOptions { DependencyName = "dep-logging", MaxRetries = 1, BaseDelayMs = 1, TimeoutMs = 1000 };

        await Assert.ThrowsAsync<ResilienceExecutionException>(() =>
            executor.ExecuteAsync<int>(options, _ => throw new HttpRequestException("transient")));

        var events = logger.Entries.Select(e => (e.Level, Event: JsonDocument.Parse(e.Message).RootElement)).ToList();
        Assert.Equal(["dependency_failure", "retry", "dependency_failure"], events.Select(e => e.Event.GetProperty("EventName").GetString()));
        Assert.All(events, e =>
        {
            Assert.Equal("order-service", e.Event.GetProperty("Service").GetString());
            Assert.Equal("corr-1", e.Event.GetProperty("CorrelationId").GetString());
            Assert.Equal("dep-logging", e.Event.GetProperty("Metadata").GetProperty("dependency").GetString());
            Assert.Equal(e.Level.ToString(), e.Event.GetProperty("Level").GetString());
        });
        Assert.Equal([LogLevel.Warning, LogLevel.Information, LogLevel.Warning], events.Select(e => e.Level));
    }

    [Fact]
    public async Task ExecuteAsync_LogsCircuitOpen_AsWarning()
    {
        var logger = new RecordingLogger<ResilienceExecutor>();
        var executor = new ResilienceExecutor(new StubCorrelationIdAccessor(), logger);
        var options = new ResilienceOptions
        {
            DependencyName = "dep-logging-circuit",
            MaxRetries = 0,
            BaseDelayMs = 1,
            TimeoutMs = 1000,
            CircuitMinThroughput = 1,
            CircuitFailureRatio = 0.1,
            CircuitBreakSeconds = 60,
        };

        await Assert.ThrowsAsync<ResilienceExecutionException>(() =>
            executor.ExecuteAsync<int>(options, _ => throw new HttpRequestException("down")));
        await Assert.ThrowsAsync<ResilienceExecutionException>(() =>
            executor.ExecuteAsync(options, _ => Task.FromResult(1)));

        var names = logger.Entries.Select(e => (e.Level, Name: JsonDocument.Parse(e.Message).RootElement.GetProperty("EventName").GetString())).ToList();
        Assert.Contains((LogLevel.Warning, "circuit_opened"), names);
        Assert.Contains((LogLevel.Warning, "circuit_open"), names);
    }

    [Fact]
    public async Task ExecuteAsync_UsesTheEntryAssemblyName_WhenNoServiceNameIsGiven()
    {
        var logger = new RecordingLogger<ResilienceExecutor>();
        var executor = new ResilienceExecutor(new StubCorrelationIdAccessor(), logger, new ResilienceLoggingOptions { ServiceName = " " });
        var options = new ResilienceOptions { DependencyName = "dep-logging-default", MaxRetries = 0, BaseDelayMs = 1, TimeoutMs = 1000 };

        await Assert.ThrowsAsync<ResilienceExecutionException>(() =>
            executor.ExecuteAsync<int>(options, _ => throw new HttpRequestException("transient")));

        var expected = Assembly.GetEntryAssembly()?.GetName().Name ?? "unknown-service";
        var evt = JsonDocument.Parse(Assert.Single(logger.Entries).Message).RootElement;
        Assert.Equal(expected, evt.GetProperty("Service").GetString());
        Assert.NotEqual("chassis-core", evt.GetProperty("Service").GetString());
    }

    [Fact]
    public async Task ExecuteAsync_WritesNothing_WhenTheLevelIsDisabled()
    {
        var logger = new RecordingLogger<ResilienceExecutor> { MinimumLevel = LogLevel.Error };
        var executor = new ResilienceExecutor(new StubCorrelationIdAccessor(), logger);
        var options = new ResilienceOptions { DependencyName = "dep-logging-disabled", MaxRetries = 1, BaseDelayMs = 1, TimeoutMs = 1000 };

        await Assert.ThrowsAsync<ResilienceExecutionException>(() =>
            executor.ExecuteAsync<int>(options, _ => throw new HttpRequestException("transient")));

        Assert.Empty(logger.Entries);
    }
}
