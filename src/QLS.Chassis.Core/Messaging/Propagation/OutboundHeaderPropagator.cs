using System.Security.Cryptography;
using System.Text;
using QLS.Chassis.Core.Configuration;
using QLS.Chassis.Core.Messaging.Headers;
using QLS.Chassis.Core.Resilience;

namespace QLS.Chassis.Core.Messaging.Propagation;

/// <summary>
/// Default <see cref="IOutboundHeaderPropagator"/>. It uses both inbound trace headers when present, builds the missing
/// one from the other when only one is valid, then falls back to the <c>_X_AMZN_TRACE_ID</c> environment variable, and
/// finally to a trace derived from a hash of the correlation ID.
/// </summary>
public sealed class OutboundHeaderPropagator : IOutboundHeaderPropagator
{
    private const string XRayEnvName = "_X_AMZN_TRACE_ID";

    private readonly ICorrelationIdAccessor _correlationIdAccessor;
    private readonly IEnvironmentProvider _environmentProvider;
    private readonly ITraceHeaderAccessor _traceHeaderAccessor;

    /// <summary>
    /// Creates a propagator with no inbound trace headers (<see cref="NullTraceHeaderAccessor"/>).
    /// </summary>
    /// <param name="correlationIdAccessor">Supplies the current correlation ID.</param>
    /// <param name="environmentProvider">Reads the <c>_X_AMZN_TRACE_ID</c> environment variable.</param>
    public OutboundHeaderPropagator(ICorrelationIdAccessor correlationIdAccessor, IEnvironmentProvider environmentProvider)
        : this(correlationIdAccessor, environmentProvider, new NullTraceHeaderAccessor())
    {
    }

    /// <summary>
    /// Creates a propagator that continues the inbound request's trace.
    /// </summary>
    /// <param name="correlationIdAccessor">Supplies the current correlation ID.</param>
    /// <param name="environmentProvider">Reads the <c>_X_AMZN_TRACE_ID</c> environment variable.</param>
    /// <param name="traceHeaderAccessor">Supplies the inbound trace headers.</param>
    public OutboundHeaderPropagator(
        ICorrelationIdAccessor correlationIdAccessor,
        IEnvironmentProvider environmentProvider,
        ITraceHeaderAccessor traceHeaderAccessor)
    {
        _correlationIdAccessor = correlationIdAccessor;
        _environmentProvider = environmentProvider;
        _traceHeaderAccessor = traceHeaderAccessor;
    }

    /// <inheritdoc />
    public void Apply(HttpRequestMessage request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var correlationId = (_correlationIdAccessor.CorrelationId ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(correlationId))
            throw new OutboundHeaderPropagationException("CorrelationId is required for outbound propagation.");

        var traceContext = ResolveTraceContext(
            correlationId,
            _traceHeaderAccessor.XAmznTraceId,
            _traceHeaderAccessor.TraceParent,
            _environmentProvider.Get(XRayEnvName));

        UpsertHeader(request, ChassisHeaderNames.CorrelationId, correlationId);
        UpsertHeader(request, ChassisHeaderNames.XAmznTraceId, traceContext.XRayHeader);
        UpsertHeader(request, ChassisHeaderNames.TraceParent, traceContext.TraceParent);
    }

    private static (string XRayHeader, string TraceParent) ResolveTraceContext(
        string correlationId,
        string? inboundXRay,
        string? inboundTraceParent,
        string? envXRay)
    {
        // Prefer the trace this request arrived with, so the outgoing call joins it. X-Ray and W3C traceparent carry the
        // same 128-bit trace ID in different formats, so when only one arrived the other is built from it.
        if (!string.IsNullOrWhiteSpace(inboundXRay) && !string.IsNullOrWhiteSpace(inboundTraceParent))
            return (inboundXRay!, inboundTraceParent!);

        if (!string.IsNullOrWhiteSpace(inboundXRay) && TryBuildTraceParentFromXRay(inboundXRay!, out var traceParentFromInboundXRay))
            return (inboundXRay!, traceParentFromInboundXRay);

        if (!string.IsNullOrWhiteSpace(inboundTraceParent) && TryBuildXRayFromTraceParent(inboundTraceParent!, out var xrayFromInboundTraceParent))
            return (xrayFromInboundTraceParent, inboundTraceParent!);

        // AWS Lambda puts the invocation's trace in _X_AMZN_TRACE_ID.
        if (!string.IsNullOrWhiteSpace(envXRay) && TryBuildTraceParentFromXRay(envXRay!, out var traceParentFromEnvXRay))
            return (envXRay!, traceParentFromEnvXRay);

        // No trace anywhere: derive one from the correlation ID, so every call made for this request shares a trace.
        var traceId = ToHex(Hash($"trace:{correlationId}"), 32);
        var spanId = ToHex(Hash($"span:{correlationId}"), 16);
        var syntheticTraceParent = $"00-{traceId}-{spanId}-01";
        var syntheticXRay = $"Root=1-{traceId[..8]}-{traceId[8..]};Parent={spanId};Sampled=1";
        return (syntheticXRay, syntheticTraceParent);
    }

    private static bool TryBuildTraceParentFromXRay(string xrayHeader, out string traceParent)
    {
        traceParent = string.Empty;
        var parts = xrayHeader.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in parts)
        {
            var idx = part.IndexOf('=');
            if (idx <= 0 || idx == part.Length - 1)
                continue;

            map[part[..idx]] = part[(idx + 1)..];
        }

        if (!map.TryGetValue("Root", out var root))
            return false;

        var rootParts = root.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (rootParts.Length != 3)
            return false;

        var traceId = rootParts[1] + rootParts[2];
        if (traceId.Length != 32 || !IsHex(traceId))
            return false;

        var parent = map.TryGetValue("Parent", out var p) && p.Length == 16 && IsHex(p)
            ? p
            : ToHex(Hash($"parent:{traceId}"), 16);

        var sampled = map.TryGetValue("Sampled", out var sampledValue) && sampledValue == "0" ? "00" : "01";
        traceParent = $"00-{traceId}-{parent}-{sampled}";
        return true;
    }

    private static bool TryBuildXRayFromTraceParent(string traceParent, out string xrayHeader)
    {
        xrayHeader = string.Empty;
        var parts = traceParent.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 4)
            return false;

        var traceId = parts[1];
        var spanId = parts[2];
        var flags = parts[3];

        if (traceId.Length != 32 || spanId.Length != 16 || flags.Length != 2)
            return false;

        if (!IsHex(traceId) || !IsHex(spanId) || !IsHex(flags))
            return false;

        var sampled = string.Equals(flags, "00", StringComparison.OrdinalIgnoreCase) ? "0" : "1";
        xrayHeader = $"Root=1-{traceId[..8]}-{traceId[8..]};Parent={spanId};Sampled={sampled}";
        return true;
    }

    private static byte[] Hash(string value)
    {
        return SHA256.HashData(Encoding.UTF8.GetBytes(value));
    }

    private static string ToHex(byte[] bytes, int length)
    {
        return Convert.ToHexString(bytes).ToLowerInvariant()[..length];
    }

    private static bool IsHex(string value)
    {
        foreach (var ch in value)
        {
            var isHex =
                (ch >= '0' && ch <= '9') ||
                (ch >= 'a' && ch <= 'f') ||
                (ch >= 'A' && ch <= 'F');
            if (!isHex)
                return false;
        }

        return true;
    }

    private static void UpsertHeader(HttpRequestMessage request, string name, string value)
    {
        request.Headers.Remove(name);
        request.Headers.TryAddWithoutValidation(name, value);
    }
}
