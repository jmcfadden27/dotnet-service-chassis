using System.Collections.Concurrent;
using Newtonsoft.Json;
using QLS.Chassis.Core.Idempotency;
using QLS.Chassis.Core.Messaging.Propagation;
using QLS.Chassis.Core.Resilience;

namespace QLS.Chassis.Core.Sample;

public sealed record OrderCreated(string OrderId, decimal Total);

public sealed record InvoiceRequested(string OrderId);

/// <summary>Supplies the current correlation ID. In a web app, QLS.Chassis.WebApi's HttpCorrelationIdAccessor does this.</summary>
public sealed class FixedCorrelationIdAccessor(string correlationId) : ICorrelationIdAccessor
{
    public string? CorrelationId => correlationId;
}

/// <summary>Pretends the current request arrived with a W3C traceparent header.</summary>
public sealed class InboundTraceHeaders : ITraceHeaderAccessor
{
    public string? XAmznTraceId => null;
    public string? TraceParent => "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01";
}

/// <summary>An in-memory <see cref="IIdempotencyStore"/>, fine for a sample or a single instance.</summary>
public sealed class InMemoryIdempotencyStore : IIdempotencyStore
{
    private sealed record Entry(string Status, string Fingerprint, string ResponseJson, long ExpiresAt);

    private readonly ConcurrentDictionary<string, Entry> _entries = new();

    public Task<bool> TryClaimAsync(string key, string fingerprint, long expiresAtEpochSeconds, long nowEpochSeconds)
    {
        var claim = new Entry("IN_PROGRESS", fingerprint, string.Empty, expiresAtEpochSeconds);
        while (true)
        {
            // Claimed: nobody had this key.
            if (_entries.TryAdd(key, claim))
                return Task.FromResult(true);
            if (!_entries.TryGetValue(key, out var existing))
                continue;
            // Someone has it and it hasn't expired: not ours.
            if (existing.ExpiresAt >= nowEpochSeconds)
                return Task.FromResult(false);
            // It expired: take it over, unless another caller just did.
            if (_entries.TryUpdate(key, claim, existing))
                return Task.FromResult(true);
        }
    }

    public Task<IdempotencyStoreRecord?> LoadAsync(string key) =>
        Task.FromResult(_entries.TryGetValue(key, out var e)
            ? new IdempotencyStoreRecord { Status = e.Status, Fingerprint = e.Fingerprint, ResponseJson = e.ResponseJson }
            : null);

    public Task SaveCompletedAsync(string key, string fingerprint, HttpIdempotencyExecutionResponse response, long expiresAtEpochSeconds)
    {
        _entries[key] = new Entry("COMPLETED", fingerprint, JsonConvert.SerializeObject(response), expiresAtEpochSeconds);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string key)
    {
        _entries.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
