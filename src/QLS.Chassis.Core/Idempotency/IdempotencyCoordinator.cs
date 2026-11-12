using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace QLS.Chassis.Core.Idempotency;

/// <summary>
/// Default <see cref="IHttpIdempotencyCoordinator"/>. Records are keyed by user (<c>anonymous</c> when blank), operation
/// name and a SHA-256 hash of the trimmed key, and fingerprinted with a SHA-256 hash of the operation, method, path,
/// sorted query string and body.
/// </summary>
public sealed class HttpIdempotencyCoordinator : IHttpIdempotencyCoordinator
{
    private const string StatusCompleted = "COMPLETED";
    private const string AnonymousUser = "anonymous";

    private readonly IIdempotencyStore _store;
    private readonly IHttpIdempotencyPolicy _policy;

    /// <summary>
    /// Creates the coordinator.
    /// </summary>
    /// <param name="store">Persists the idempotency records.</param>
    /// <param name="policy">Decides which requests need a key and which responses are kept.</param>
    public HttpIdempotencyCoordinator(IIdempotencyStore store, IHttpIdempotencyPolicy policy)
    {
        _store = store;
        _policy = policy;
    }

    /// <inheritdoc />
    public async Task<HttpIdempotencyExecutionResponse> ExecuteAsync(
        HttpIdempotencyExecutionRequest request,
        Func<Task<HttpIdempotencyExecutionResponse>> operation)
    {
        if (!_policy.RequiresForMethod(request.HttpMethod))
            return await operation();

        var idempotencyKey = ResolveHeader(request.Headers, _policy.HeaderName).Trim();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new IdempotencyKeyRequiredException($"{_policy.HeaderName} header is required for mutation requests.");

        if (idempotencyKey.Length > 128)
            throw new IdempotencyKeyRequiredException($"{_policy.HeaderName} header must be 128 characters or fewer.");

        // The record key scopes the client's key to the user and the operation, so keys never clash between them. The
        // fingerprint is what the request *was*, so reusing a key for a different request can be detected.
        var userId = string.IsNullOrWhiteSpace(request.UserId) ? AnonymousUser : request.UserId.Trim();
        var recordKey = BuildStoreKey(userId, request.OperationName, idempotencyKey);
        var fingerprint = BuildFingerprint(request);
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var expiresAt = now + (long)_policy.RecordTtl.TotalSeconds;

        // Claiming is atomic in the store, so of two identical requests arriving together exactly one runs.
        var claimed = await _store.TryClaimAsync(recordKey, fingerprint, expiresAt, now);
        if (!claimed)
        {
            // Someone holds the key: replay their result, or explain why we can't.
            var existing = await _store.LoadAsync(recordKey);
            if (existing == null)
                throw new IdempotencyRequestInProgressException("A request with this idempotency key is currently in progress.");

            if (!string.Equals(existing.Fingerprint, fingerprint, StringComparison.Ordinal))
                throw new IdempotencyKeyReuseConflictException("Idempotency key has already been used with a different request payload.");

            if (string.Equals(existing.Status, StatusCompleted, StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(existing.ResponseJson))
                return JsonConvert.DeserializeObject<HttpIdempotencyExecutionResponse>(existing.ResponseJson)!;

            throw new IdempotencyRequestInProgressException("A request with this idempotency key is currently in progress.");
        }

        try
        {
            var response = await operation();

            if (!_policy.ShouldPersistResponse(response.StatusCode))
            {
                await _store.DeleteAsync(recordKey);
                return response;
            }

            await _store.SaveCompletedAsync(recordKey, fingerprint, response, expiresAt);
            return response;
        }
        catch
        {
            // Release the claim, so the client's retry with the same key runs again rather than waiting forever.
            await _store.DeleteAsync(recordKey);
            throw;
        }
    }

    private static string ResolveHeader(IReadOnlyDictionary<string, string> headers, string headerName)
    {
        foreach (var pair in headers)
        {
            if (string.Equals(pair.Key, headerName, StringComparison.OrdinalIgnoreCase))
                return pair.Value ?? string.Empty;
        }

        return string.Empty;
    }

    private static string BuildStoreKey(string userId, string operationName, string idempotencyKey)
    {
        return $"IDEMPOTENCY#USER:{userId}#OP:{operationName}#KEY:{ToSha256(idempotencyKey)}";
    }

    private static string BuildFingerprint(HttpIdempotencyExecutionRequest request)
    {
        var query = request.QueryStringParameters == null
            ? string.Empty
            : string.Join("&", request.QueryStringParameters.OrderBy(x => x.Key).Select(x => $"{x.Key}={x.Value}"));
        return ToSha256($"{request.OperationName}|{request.HttpMethod}|{request.Path}|{query}|{request.Body}");
    }

    private static string ToSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
