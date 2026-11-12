using Newtonsoft.Json.Linq;

namespace QLS.Chassis.Core.Logging;

/// <summary>
/// Default <see cref="ILogSanitizer"/>. Redacts headers, query parameters, JSON properties (at any depth) and form fields
/// whose names, ignoring <c>-</c>, <c>_</c> and <c>.</c>, contain a sensitive fragment such as <c>token</c>,
/// <c>password</c> or <c>apikey</c>. Header and query values are cut at 256 characters and bodies at 4096.
/// </summary>
public sealed class DefaultLogSanitizer : ILogSanitizer
{
    private const int HeaderValueMaxLength = 256;
    private const int QueryValueMaxLength = 256;
    private const int BodyMaxLength = 4096;

    private static readonly string[] SensitiveKeyFragments =
    [
        "authorization",
        "cookie",
        "token",
        "secret",
        "password",
        "pwd",
        "apikey",
        "api_key",
        "credential",
        "session",
        "jwt",
    ];

    /// <inheritdoc />
    public SanitizedRequestLogData Sanitize(LogSanitizerRequest request)
    {
        var redactionApplied = false;
        var truncated = false;

        var headers = SanitizeMap(request.Headers, HeaderValueMaxLength, ref redactionApplied, ref truncated);
        var query = SanitizeMap(request.QueryStringParameters, QueryValueMaxLength, ref redactionApplied, ref truncated);

        var bodyLogged = true;
        var bodyReason = string.Empty;
        var body = string.Empty;

        if (request.IsBase64Encoded)
        {
            bodyLogged = false;
            bodyReason = "binary_or_base64";
        }
        else
        {
            var contentType = ResolveContentType(request.Headers);
            if (IsBinaryContentType(contentType))
            {
                bodyLogged = false;
                bodyReason = "binary_or_base64";
            }
            else
            {
                body = SanitizeBody(request.Body ?? string.Empty, contentType, ref redactionApplied, ref truncated);
            }
        }

        return new SanitizedRequestLogData
        {
            Headers = headers,
            QueryStringParameters = query,
            Body = bodyLogged ? body : null,
            BodyLogged = bodyLogged,
            BodyReason = bodyLogged ? null : bodyReason,
            RedactionApplied = redactionApplied,
            Truncated = truncated,
        };
    }

    private static IReadOnlyDictionary<string, string?> SanitizeMap(
        IReadOnlyDictionary<string, string>? source,
        int maxLength,
        ref bool redactionApplied,
        ref bool truncated)
    {
        if (source == null || source.Count == 0)
            return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        var sanitized = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in source)
        {
            var key = pair.Key ?? string.Empty;
            var value = pair.Value ?? string.Empty;

            if (IsSensitiveKey(key))
            {
                sanitized[key] = "[REDACTED]";
                redactionApplied = true;
                continue;
            }

            if (value.Length > maxLength)
            {
                sanitized[key] = value[..maxLength];
                truncated = true;
            }
            else
            {
                sanitized[key] = value;
            }
        }

        return sanitized;
    }

    private static string SanitizeBody(string body, string contentType, ref bool redactionApplied, ref bool truncated)
    {
        if (string.IsNullOrEmpty(body))
            return string.Empty;

        string sanitized;

        if (contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
        {
            sanitized = SanitizeJsonBody(body, ref redactionApplied);
        }
        else if (contentType.Contains("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
        {
            sanitized = SanitizeFormUrlEncodedBody(body, ref redactionApplied);
        }
        else
        {
            sanitized = "[NON_JSON_BODY_OMITTED]";
        }

        if (sanitized.Length > BodyMaxLength)
        {
            truncated = true;
            sanitized = sanitized[..BodyMaxLength];
        }

        return sanitized;
    }

    private static string SanitizeJsonBody(string body, ref bool redactionApplied)
    {
        try
        {
            var token = JToken.Parse(body);
            SanitizeJsonToken(token, ref redactionApplied);
            return token.ToString(Newtonsoft.Json.Formatting.None);
        }
        catch
        {
            return "[INVALID_JSON_BODY_OMITTED]";
        }
    }

    private static void SanitizeJsonToken(JToken token, ref bool redactionApplied)
    {
        if (token is JObject obj)
        {
            foreach (var prop in obj.Properties().ToList())
            {
                if (IsSensitiveKey(prop.Name))
                {
                    prop.Value = "[REDACTED]";
                    redactionApplied = true;
                }
                else
                {
                    SanitizeJsonToken(prop.Value, ref redactionApplied);
                }
            }
            return;
        }

        if (token is JArray arr)
        {
            foreach (var child in arr)
                SanitizeJsonToken(child, ref redactionApplied);
        }
    }

    private static string SanitizeFormUrlEncodedBody(string body, ref bool redactionApplied)
    {
        var parts = body.Split('&', StringSplitOptions.RemoveEmptyEntries);
        var sanitizedParts = new List<string>(parts.Length);
        foreach (var part in parts)
        {
            var idx = part.IndexOf('=');
            if (idx <= 0)
            {
                sanitizedParts.Add(part);
                continue;
            }

            var key = part[..idx];
            var value = part[(idx + 1)..];
            if (IsSensitiveKey(key))
            {
                sanitizedParts.Add($"{key}=[REDACTED]");
                redactionApplied = true;
            }
            else
            {
                sanitizedParts.Add($"{key}={value}");
            }
        }

        return string.Join('&', sanitizedParts);
    }

    private static bool IsSensitiveKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        var normalized = key.Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace(".", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();

        return SensitiveKeyFragments.Any(fragment => normalized.Contains(fragment, StringComparison.Ordinal));
    }

    private static string ResolveContentType(IReadOnlyDictionary<string, string>? headers)
    {
        if (headers == null)
            return string.Empty;

        foreach (var pair in headers)
        {
            if (string.Equals(pair.Key, "Content-Type", StringComparison.OrdinalIgnoreCase))
            {
                var value = pair.Value ?? string.Empty;
                var semicolon = value.IndexOf(';');
                return semicolon > 0 ? value[..semicolon].Trim() : value.Trim();
            }
        }

        return string.Empty;
    }

    private static bool IsBinaryContentType(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return false;

        return contentType.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase)
            || contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            || contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)
            || contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase)
            || string.Equals(contentType, "application/octet-stream", StringComparison.OrdinalIgnoreCase)
            || string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase)
            || string.Equals(contentType, "application/zip", StringComparison.OrdinalIgnoreCase)
            || string.Equals(contentType, "application/gzip", StringComparison.OrdinalIgnoreCase);
    }
}
