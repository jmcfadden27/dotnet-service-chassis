using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace QLS.Chassis.Core.Idempotency;

/// <summary>
/// Default <see cref="IMessageIdempotencyCoordinator"/>. Records are keyed by consumer name (<c>consumer</c> when blank),
/// operation name and a SHA-256 hash of the trimmed key, and fingerprinted with a SHA-256 hash of the consumer name,
/// operation, key and message body. Only the response's status code and body are stored.
/// </summary>
public sealed class MessageIdempotencyCoordinator : IMessageIdempotencyCoordinator
{
    private const string StatusCompleted = "COMPLETED";

    private readonly IIdempotencyStore _store;
    private readonly IMessageIdempotencyPolicy _policy;

    /// <summary>
    /// Creates the coordinator.
    /// </summary>
    /// <param name="store">Persists the idempotency records.</param>
    /// <param name="policy">Decides how long records live and which responses are kept.</param>
    public MessageIdempotencyCoordinator(IIdempotencyStore store, IMessageIdempotencyPolicy policy)
    {
        _store = store;
        _policy = policy;
    }

    /// <inheritdoc />
    public async Task<MessageIdempotencyExecutionResponse> ExecuteAsync(
        MessageIdempotencyExecutionRequest request,
        Func<Task<MessageIdempotencyExecutionResponse>> operation)
    {
        var idempotencyKey = (request.IdempotencyKey ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new IdempotencyKeyRequiredException("Message idempotency key is required.");

        if (idempotencyKey.Length > 128)
            throw new IdempotencyKeyRequiredException("Message idempotency key must be 128 characters or fewer.");

        var consumerName = string.IsNullOrWhiteSpace(request.ConsumerName) ? "consumer" : request.ConsumerName.Trim();
        var recordKey = BuildStoreKey(consumerName, request.OperationName, idempotencyKey);
        var fingerprint = BuildFingerprint(request);
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var expiresAt = now + (long)_policy.RecordTtl.TotalSeconds;

        var claimed = await _store.TryClaimAsync(recordKey, fingerprint, expiresAt, now);
        if (!claimed)
        {
            var existing = await _store.LoadAsync(recordKey);
            if (existing == null)
                throw new IdempotencyRequestInProgressException("A message with this idempotency key is currently in progress.");

            if (!string.Equals(existing.Fingerprint, fingerprint, StringComparison.Ordinal))
                throw new IdempotencyKeyReuseConflictException("Idempotency key has already been used with a different message payload.");

            if (string.Equals(existing.Status, StatusCompleted, StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(existing.ResponseJson))
                return JsonConvert.DeserializeObject<MessageIdempotencyExecutionResponse>(existing.ResponseJson)!;

            throw new IdempotencyRequestInProgressException("A message with this idempotency key is currently in progress.");
        }

        try
        {
            var response = await operation();

            if (!_policy.ShouldPersistResponse(response.StatusCode))
            {
                await _store.DeleteAsync(recordKey);
                return response;
            }

            await _store.SaveCompletedAsync(recordKey, fingerprint, new HttpIdempotencyExecutionResponse
            {
                StatusCode = response.StatusCode,
                Body = response.Body,
            }, expiresAt);
            return response;
        }
        catch
        {
            await _store.DeleteAsync(recordKey);
            throw;
        }
    }

    private static string BuildStoreKey(string consumerName, string operationName, string idempotencyKey)
    {
        return $"IDEMPOTENCY#CONSUMER:{consumerName}#OP:{operationName}#KEY:{ToSha256(idempotencyKey)}";
    }

    private static string BuildFingerprint(MessageIdempotencyExecutionRequest request)
    {
        return ToSha256($"{request.ConsumerName}|{request.OperationName}|{request.IdempotencyKey}|{request.MessageBody}");
    }

    private static string ToSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
