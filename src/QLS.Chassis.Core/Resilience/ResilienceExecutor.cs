using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using QLS.Chassis.Core.Logging;

namespace QLS.Chassis.Core.Resilience;

/// <summary>
/// Default <see cref="IResilienceExecutor"/>. It keeps one circuit per dependency name (compared case-insensitively),
/// so register it as a singleton. Each retry, failure and circuit change is logged as a JSON
/// <see cref="StructuredLogEvent"/>.
/// </summary>
public sealed class ResilienceExecutor : IResilienceExecutor
{
    private readonly ConcurrentDictionary<string, CircuitState> _circuits = new(StringComparer.OrdinalIgnoreCase);
    private readonly ICorrelationIdAccessor _correlationIdAccessor;
    private readonly ILogger<ResilienceExecutor> _logger;
    private readonly string _serviceName;

    /// <summary>
    /// Creates the executor.
    /// </summary>
    /// <param name="correlationIdAccessor">Supplies the correlation ID written to each log event.</param>
    /// <param name="logger">The logger for resilience events.</param>
    /// <param name="loggingOptions">Sets the service name in log events. When omitted or blank, the entry assembly's name is used.</param>
    public ResilienceExecutor(
        ICorrelationIdAccessor correlationIdAccessor,
        ILogger<ResilienceExecutor> logger,
        ResilienceLoggingOptions? loggingOptions = null)
    {
        _correlationIdAccessor = correlationIdAccessor;
        _logger = logger;
        _serviceName = string.IsNullOrWhiteSpace(loggingOptions?.ServiceName)
            ? Assembly.GetEntryAssembly()?.GetName().Name ?? "unknown-service"
            : loggingOptions.ServiceName;
    }

    /// <inheritdoc />
    public async Task<T> ExecuteAsync<T>(ResilienceOptions options, Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        // One circuit per dependency name, shared by every call to that dependency.
        var circuit = _circuits.GetOrAdd(options.DependencyName, _ => new CircuitState());

        for (var attempt = 0; ; attempt++)
        {
            // Asked before every attempt, retries included: once the circuit opens, the remaining retries fail fast too.
            var now = DateTime.UtcNow;
            if (!circuit.TryAcquireExecutionSlot(now, out var retryAfterSeconds))
            {
                LogEvent("circuit_open", options.DependencyName, attempt + 1, null, retryAfterSeconds);
                throw new ResilienceExecutionException(
                    options.DependencyName,
                    ResilienceFailureType.CircuitOpen,
                    $"Circuit for dependency '{options.DependencyName}' is open.",
                    retryAfterSeconds: retryAfterSeconds ?? options.RetryAfterSeconds);
            }

            try
            {
                // The operation's token fires on this attempt's timeout or on the caller's cancellation, whichever is first.
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(options.TimeoutMs));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                var result = await operation(linkedCts.Token);
                var closedFromHalfOpen = circuit.RecordSuccess();

                if (closedFromHalfOpen)
                    LogEvent("circuit_closed", options.DependencyName, attempt + 1, null, null);

                return result;
            }
            // Cancelled but not by the caller: our per-attempt timeout fired.
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                var transition = circuit.RecordFailure(options, DateTime.UtcNow);
                LogFailureAndTransition("timeout", options.DependencyName, attempt + 1, transition, ex);

                if (attempt < options.MaxRetries)
                {
                    LogEvent("retry", options.DependencyName, attempt + 1, nameof(OperationCanceledException), null);
                    await DelayWithJitter(attempt, options, cancellationToken);
                    continue;
                }

                throw new ResilienceExecutionException(
                    options.DependencyName,
                    ResilienceFailureType.Timeout,
                    $"Dependency '{options.DependencyName}' timed out.",
                    ex,
                    options.RetryAfterSeconds);
            }
            catch (Exception ex) when (IsTransient(ex, options))
            {
                var transition = circuit.RecordFailure(options, DateTime.UtcNow);
                LogFailureAndTransition("dependency_failure", options.DependencyName, attempt + 1, transition, ex);

                if (attempt < options.MaxRetries)
                {
                    LogEvent("retry", options.DependencyName, attempt + 1, ex.GetType().Name, null);
                    await DelayWithJitter(attempt, options, cancellationToken);
                    continue;
                }

                throw new ResilienceExecutionException(
                    options.DependencyName,
                    ResilienceFailureType.DependencyUnavailable,
                    $"Dependency '{options.DependencyName}' is unavailable.",
                    ex,
                    options.RetryAfterSeconds);
            }
            catch
            {
                // Not transient (a validation error, a 404 turned into an exception): the dependency answered, so it
                // counts as healthy for the circuit, and the exception goes to the caller without a retry.
                circuit.RecordSuccess();
                throw;
            }
        }
    }

    private static bool IsTransient(Exception ex, ResilienceOptions options)
    {
        if (options.TransientExceptionClassifier?.Invoke(ex) == true)
            return true;

        if (ex is HttpRequestException)
            return true;

        if (ex is TaskCanceledException)
            return true;

        return false;
    }

    private static async Task DelayWithJitter(int attempt, ResilienceOptions options, CancellationToken cancellationToken)
    {
        var exponential = options.BaseDelayMs * Math.Pow(2, attempt);
        var jitter = Random.Shared.Next(0, Math.Max(1, options.BaseDelayMs / 2));
        var delayMs = (int)Math.Min(5000, exponential + jitter);
        await Task.Delay(delayMs, cancellationToken);
    }

    private void LogFailureAndTransition(string eventName, string dependencyName, int attempt, CircuitTransition transition, Exception ex)
    {
        LogEvent(eventName, dependencyName, attempt, ex.GetType().Name, transition.RetryAfterSeconds);

        if (transition.StateOpened)
            LogEvent("circuit_opened", dependencyName, attempt, ex.GetType().Name, transition.RetryAfterSeconds);
    }

    private void LogEvent(string eventName, string dependencyName, int attempt, string? reason, int? retryAfterSeconds)
    {
        var level = IsFailureEvent(eventName) ? LogLevel.Warning : LogLevel.Information;
        if (!_logger.IsEnabled(level))
            return;

        var evt = new StructuredLogEvent
        {
            Level = level.ToString(),
            EventName = eventName,
            Service = _serviceName,
            Operation = "resilience_execute",
            CorrelationId = _correlationIdAccessor.CorrelationId,
            Metadata = new Dictionary<string, object?>
            {
                ["dependency"] = dependencyName,
                ["attempt"] = attempt,
                ["reason"] = reason,
                ["retryAfterSeconds"] = retryAfterSeconds,
                ["timestampUtc"] = DateTime.UtcNow.ToString("o"),
            }
        };

        _logger.Log(level, "{StructuredEvent}", JsonSerializer.Serialize(evt));
    }

    private static bool IsFailureEvent(string eventName)
    {
        return string.Equals(eventName, "timeout", StringComparison.OrdinalIgnoreCase)
               || string.Equals(eventName, "dependency_failure", StringComparison.OrdinalIgnoreCase)
               || string.Equals(eventName, "circuit_open", StringComparison.OrdinalIgnoreCase)
               || string.Equals(eventName, "circuit_opened", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class CircuitState
    {
        private readonly object _gate = new();
        private CircuitMode _mode = CircuitMode.Closed;
        private DateTime _openUntilUtc = DateTime.MinValue;
        private bool _halfOpenExecutionInFlight;
        private int _sampleCount;
        private int _failureCount;

        public bool TryAcquireExecutionSlot(DateTime nowUtc, out int? retryAfterSeconds)
        {
            retryAfterSeconds = null;

            lock (_gate)
            {
                // Open: refuse calls until the break time has passed, then move to half-open.
                if (_mode == CircuitMode.Open)
                {
                    if (nowUtc < _openUntilUtc)
                    {
                        retryAfterSeconds = (int)Math.Ceiling((_openUntilUtc - nowUtc).TotalSeconds);
                        return false;
                    }

                    _mode = CircuitMode.HalfOpen;
                    _halfOpenExecutionInFlight = false;
                }

                // Half-open: let exactly one probe call through. Everyone else waits for its result.
                if (_mode == CircuitMode.HalfOpen)
                {
                    if (_halfOpenExecutionInFlight)
                    {
                        retryAfterSeconds = 1;
                        return false;
                    }

                    _halfOpenExecutionInFlight = true;
                    return true;
                }

                return true;
            }
        }

        public bool RecordSuccess()
        {
            lock (_gate)
            {
                // The probe succeeded: the dependency is back, so close the circuit and start counting afresh.
                if (_mode == CircuitMode.HalfOpen)
                {
                    _mode = CircuitMode.Closed;
                    _halfOpenExecutionInFlight = false;
                    _sampleCount = 0;
                    _failureCount = 0;
                    return true;
                }

                if (_mode == CircuitMode.Closed)
                {
                    _sampleCount++;
                    TrimIfNeeded();
                }

                return false;
            }
        }

        public CircuitTransition RecordFailure(ResilienceOptions options, DateTime nowUtc)
        {
            lock (_gate)
            {
                // The probe failed: the dependency is still down, so open the circuit for another break.
                if (_mode == CircuitMode.HalfOpen)
                {
                    _mode = CircuitMode.Open;
                    _halfOpenExecutionInFlight = false;
                    _openUntilUtc = nowUtc.AddSeconds(options.CircuitBreakSeconds);
                    _sampleCount = 0;
                    _failureCount = 0;
                    return new CircuitTransition
                    {
                        StateOpened = true,
                        RetryAfterSeconds = options.CircuitBreakSeconds,
                    };
                }

                _sampleCount++;
                _failureCount++;

                // Open only once enough calls have been seen, so a couple of early failures can't trip it.
                var ratio = _sampleCount == 0 ? 0d : (double)_failureCount / _sampleCount;
                if (_sampleCount >= options.CircuitMinThroughput && ratio >= options.CircuitFailureRatio)
                {
                    _mode = CircuitMode.Open;
                    _openUntilUtc = nowUtc.AddSeconds(options.CircuitBreakSeconds);
                    _sampleCount = 0;
                    _failureCount = 0;
                    return new CircuitTransition
                    {
                        StateOpened = true,
                        RetryAfterSeconds = options.CircuitBreakSeconds,
                    };
                }

                TrimIfNeeded();

                return new CircuitTransition
                {
                    StateOpened = false,
                    RetryAfterSeconds = null,
                };
            }
        }

        // Halves both counts at 100 samples, so the failure ratio follows recent calls rather than the whole history.
        private void TrimIfNeeded()
        {
            if (_sampleCount < 100)
                return;

            _sampleCount /= 2;
            _failureCount /= 2;
        }
    }

    private enum CircuitMode
    {
        Closed,
        Open,
        HalfOpen,
    }

    private sealed class CircuitTransition
    {
        public bool StateOpened { get; init; }
        public int? RetryAfterSeconds { get; init; }
    }
}
