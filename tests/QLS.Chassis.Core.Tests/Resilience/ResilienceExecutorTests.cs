using Microsoft.Extensions.Logging.Abstractions;
using QLS.Chassis.Core.Resilience;

namespace QLS.Chassis.Core.Tests.Resilience;

public sealed class ResilienceExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsResult_OnSuccess()
    {
        var executor = new ResilienceExecutor(new StubCorrelationIdAccessor { CorrelationId = "corr-1" }, NullLogger<ResilienceExecutor>.Instance);
        var options = CreateOptions("dep-success");

        var result = await executor.ExecuteAsync(options, _ => Task.FromResult(123));

        Assert.Equal(123, result);
    }

    [Fact]
    public async Task ExecuteAsync_RetriesTransientFailures_ThenThrowsDependencyUnavailable()
    {
        var executor = new ResilienceExecutor(new StubCorrelationIdAccessor(), NullLogger<ResilienceExecutor>.Instance);
        var options = CreateOptions("dep-transient", maxRetries: 2, baseDelayMs: 1);
        var attempts = 0;

        var ex = await Assert.ThrowsAsync<ResilienceExecutionException>(() =>
            executor.ExecuteAsync<int>(options, _ =>
            {
                attempts++;
                throw new HttpRequestException("transient");
            }));

        Assert.Equal(3, attempts);
        Assert.Equal(ResilienceFailureType.DependencyUnavailable, ex.FailureType);
    }

    [Fact]
    public async Task ExecuteAsync_TreatsTaskCanceledException_AsTransient_WhenCallerTokenAlreadyCanceled()
    {
        var executor = new ResilienceExecutor(new StubCorrelationIdAccessor(), NullLogger<ResilienceExecutor>.Instance);
        var options = CreateOptions("dep-task-canceled", maxRetries: 0, baseDelayMs: 1);
        var attempts = 0;
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var ex = await Assert.ThrowsAsync<ResilienceExecutionException>(() =>
            executor.ExecuteAsync<int>(options, _ =>
            {
                attempts++;
                throw new TaskCanceledException("canceled");
            }, cts.Token));

        Assert.Equal(1, attempts);
        Assert.Equal(ResilienceFailureType.DependencyUnavailable, ex.FailureType);
    }

    [Fact]
    public async Task ExecuteAsync_TimesOut_AndThrowsTimeoutFailure()
    {
        var executor = new ResilienceExecutor(new StubCorrelationIdAccessor(), NullLogger<ResilienceExecutor>.Instance);
        var options = CreateOptions("dep-timeout", timeoutMs: 20, maxRetries: 1, baseDelayMs: 1);
        var attempts = 0;

        var ex = await Assert.ThrowsAsync<ResilienceExecutionException>(() =>
            executor.ExecuteAsync<int>(options, async ct =>
            {
                attempts++;
                await Task.Delay(TimeSpan.FromSeconds(1), ct);
                return 1;
            }));

        Assert.Equal(2, attempts);
        Assert.Equal(ResilienceFailureType.Timeout, ex.FailureType);
    }

    [Fact]
    public async Task ExecuteAsync_OpensCircuit_ThenFailsFastWithCircuitOpen()
    {
        var executor = new ResilienceExecutor(new StubCorrelationIdAccessor(), NullLogger<ResilienceExecutor>.Instance);
        var options = CreateOptions("dep-circuit", maxRetries: 0, circuitMinThroughput: 1, circuitFailureRatio: 1.0, circuitBreakSeconds: 60);

        await Assert.ThrowsAsync<ResilienceExecutionException>(() =>
            executor.ExecuteAsync<int>(options, _ => throw new HttpRequestException("boom")));

        var ex = await Assert.ThrowsAsync<ResilienceExecutionException>(() =>
            executor.ExecuteAsync<int>(options, _ => Task.FromResult(1)));

        Assert.Equal(ResilienceFailureType.CircuitOpen, ex.FailureType);
    }

    [Fact]
    public async Task ExecuteAsync_HalfOpenSucceeds_AndClosesCircuit()
    {
        var executor = new ResilienceExecutor(new StubCorrelationIdAccessor(), NullLogger<ResilienceExecutor>.Instance);
        var options = CreateOptions("dep-half-open", maxRetries: 0, circuitMinThroughput: 1, circuitFailureRatio: 1.0, circuitBreakSeconds: 1);

        await Assert.ThrowsAsync<ResilienceExecutionException>(() =>
            executor.ExecuteAsync<int>(options, _ => throw new HttpRequestException("boom")));

        await Task.Delay(1100);

        var result = await executor.ExecuteAsync(options, _ => Task.FromResult(7));
        Assert.Equal(7, result);

        var second = await executor.ExecuteAsync(options, _ => Task.FromResult(8));
        Assert.Equal(8, second);
    }

    [Fact]
    public async Task ExecuteAsync_HalfOpenFailure_ReopensCircuit()
    {
        var executor = new ResilienceExecutor(new StubCorrelationIdAccessor(), NullLogger<ResilienceExecutor>.Instance);
        var options = CreateOptions("dep-half-open-fail", maxRetries: 0, circuitMinThroughput: 1, circuitFailureRatio: 1.0, circuitBreakSeconds: 1);

        await Assert.ThrowsAsync<ResilienceExecutionException>(() =>
            executor.ExecuteAsync<int>(options, _ => throw new HttpRequestException("boom")));

        await Task.Delay(1100);

        await Assert.ThrowsAsync<ResilienceExecutionException>(() =>
            executor.ExecuteAsync<int>(options, _ => throw new HttpRequestException("still bad")));

        var circuitOpen = await Assert.ThrowsAsync<ResilienceExecutionException>(() =>
            executor.ExecuteAsync<int>(options, _ => Task.FromResult(1)));
        Assert.Equal(ResilienceFailureType.CircuitOpen, circuitOpen.FailureType);
    }

    [Fact]
    public async Task ExecuteAsync_HalfOpen_AllowsSingleInFlightProbe()
    {
        var executor = new ResilienceExecutor(new StubCorrelationIdAccessor(), NullLogger<ResilienceExecutor>.Instance);
        var options = CreateOptions("dep-half-open-inflight", maxRetries: 0, circuitMinThroughput: 1, circuitFailureRatio: 1.0, circuitBreakSeconds: 1);

        await Assert.ThrowsAsync<ResilienceExecutionException>(() =>
            executor.ExecuteAsync<int>(options, _ => throw new HttpRequestException("boom")));

        await Task.Delay(1100);

        var releaseProbe = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstProbe = executor.ExecuteAsync(options, async _ =>
        {
            await releaseProbe.Task;
            return 5;
        });

        await Task.Delay(20);

        var secondProbe = await Assert.ThrowsAsync<ResilienceExecutionException>(() =>
            executor.ExecuteAsync<int>(options, _ => Task.FromResult(6)));
        Assert.Equal(ResilienceFailureType.CircuitOpen, secondProbe.FailureType);

        releaseProbe.SetResult(true);
        var result = await firstProbe;
        Assert.Equal(5, result);
    }

    [Fact]
    public async Task ExecuteAsync_TrimWindowPath_IsExercised()
    {
        var executor = new ResilienceExecutor(new StubCorrelationIdAccessor(), NullLogger<ResilienceExecutor>.Instance);
        var options = CreateOptions("dep-trim", maxRetries: 0);

        for (var i = 0; i < 105; i++)
            await executor.ExecuteAsync(options, _ => Task.FromResult(i));

        var value = await executor.ExecuteAsync(options, _ => Task.FromResult(999));
        Assert.Equal(999, value);
    }

    [Fact]
    public async Task ExecuteAsync_BubblesNonTransientException()
    {
        var executor = new ResilienceExecutor(new StubCorrelationIdAccessor(), NullLogger<ResilienceExecutor>.Instance);
        var options = CreateOptions("dep-non-transient");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            executor.ExecuteAsync<int>(options, _ => throw new InvalidOperationException("hard fail")));

        Assert.Equal("hard fail", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_RespectsCallerCancellationToken()
    {
        var executor = new ResilienceExecutor(new StubCorrelationIdAccessor(), NullLogger<ResilienceExecutor>.Instance);
        var options = CreateOptions("dep-cancel");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            executor.ExecuteAsync<int>(options, ct =>
            {
                ct.ThrowIfCancellationRequested();
                return Task.FromResult(1);
            }, cts.Token));
    }

    private static ResilienceOptions CreateOptions(
        string dependencyName,
        int timeoutMs = 500,
        int maxRetries = 0,
        int baseDelayMs = 1,
        double circuitFailureRatio = 0.5,
        int circuitMinThroughput = 10,
        int circuitBreakSeconds = 30)
    {
        return new ResilienceOptions
        {
            DependencyName = dependencyName,
            TimeoutMs = timeoutMs,
            MaxRetries = maxRetries,
            BaseDelayMs = baseDelayMs,
            CircuitFailureRatio = circuitFailureRatio,
            CircuitMinThroughput = circuitMinThroughput,
            CircuitBreakSeconds = circuitBreakSeconds,
            RetryAfterSeconds = 5,
        };
    }
}
