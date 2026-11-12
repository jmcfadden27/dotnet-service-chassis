using QLS.Chassis.Core.Logging;

namespace QLS.Chassis.Core.Tests.Logging;

public sealed class DefaultLogSanitizerTests
{
    [Fact]
    public void Sanitize_RedactsSensitiveHeadersQueryAndJsonBody()
    {
        var sanitizer = new DefaultLogSanitizer();

        var result = sanitizer.Sanitize(new LogSanitizerRequest
        {
            Headers = new Dictionary<string, string>
            {
                ["Authorization"] = "Bearer abc",
                ["Content-Type"] = "application/json",
            },
            QueryStringParameters = new Dictionary<string, string>
            {
                ["token"] = "xyz"
            },
            Body = "{\"name\":\"A\",\"password\":\"secret\"}",
            IsBase64Encoded = false
        });

        Assert.Equal("[REDACTED]", result.Headers["Authorization"]);
        Assert.Equal("[REDACTED]", result.QueryStringParameters["token"]);
        Assert.Contains("[REDACTED]", result.Body);
        Assert.True(result.RedactionApplied);
    }

    [Fact]
    public void Sanitize_DoesNotLogBinaryBody()
    {
        var sanitizer = new DefaultLogSanitizer();

        var result = sanitizer.Sanitize(new LogSanitizerRequest
        {
            Headers = new Dictionary<string, string> { ["Content-Type"] = "application/octet-stream" },
            Body = "SGVsbG8=",
            IsBase64Encoded = true
        });

        Assert.False(result.BodyLogged);
        Assert.Null(result.Body);
        Assert.Equal("binary_or_base64", result.BodyReason);
    }

    [Fact]
    public void Sanitize_TruncatesLargeValues()
    {
        var sanitizer = new DefaultLogSanitizer();
        var huge = new string('a', 10000);

        var result = sanitizer.Sanitize(new LogSanitizerRequest
        {
            Headers = new Dictionary<string, string> { ["X-Test"] = huge, ["Content-Type"] = "application/json" },
            Body = $"{{\"note\":\"{huge}\"}}",
            IsBase64Encoded = false
        });

        Assert.True(result.Truncated);
        Assert.NotNull(result.Body);
        Assert.True(result.Body!.Length <= 4096);
        Assert.True(result.Headers["X-Test"]!.Length <= 256);
    }

    [Fact]
    public void Sanitize_RedactsFormEncodedSensitiveKeys()
    {
        var sanitizer = new DefaultLogSanitizer();

        var result = sanitizer.Sanitize(new LogSanitizerRequest
        {
            Headers = new Dictionary<string, string> { ["Content-Type"] = "application/x-www-form-urlencoded;charset=utf-8" },
            Body = "email=a@example.com&api_key=abc123",
            IsBase64Encoded = false
        });

        Assert.True(result.BodyLogged);
        Assert.Equal("email=a@example.com&api_key=[REDACTED]", result.Body);
        Assert.True(result.RedactionApplied);
    }

    [Fact]
    public void Sanitize_InvalidJsonBody_ReturnsPlaceholder()
    {
        var sanitizer = new DefaultLogSanitizer();

        var result = sanitizer.Sanitize(new LogSanitizerRequest
        {
            Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" },
            Body = "{not-json",
            IsBase64Encoded = false
        });

        Assert.True(result.BodyLogged);
        Assert.Equal("[INVALID_JSON_BODY_OMITTED]", result.Body);
    }

    [Theory]
    [InlineData("text/plain")]
    [InlineData("application/xml")]
    public void Sanitize_NonJsonBody_OmitsBodyContent(string contentType)
    {
        var sanitizer = new DefaultLogSanitizer();

        var result = sanitizer.Sanitize(new LogSanitizerRequest
        {
            Headers = new Dictionary<string, string> { ["Content-Type"] = contentType },
            Body = "<secret>value</secret>",
            IsBase64Encoded = false
        });

        Assert.True(result.BodyLogged);
        Assert.Equal("[NON_JSON_BODY_OMITTED]", result.Body);
    }

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("image/png")]
    [InlineData("multipart/form-data")]
    public void Sanitize_BinaryContentType_DoesNotLogBody(string contentType)
    {
        var sanitizer = new DefaultLogSanitizer();

        var result = sanitizer.Sanitize(new LogSanitizerRequest
        {
            Headers = new Dictionary<string, string> { ["Content-Type"] = contentType },
            Body = "ignored",
            IsBase64Encoded = false
        });

        Assert.False(result.BodyLogged);
        Assert.Null(result.Body);
        Assert.Equal("binary_or_base64", result.BodyReason);
    }

    [Fact]
    public void Sanitize_HandlesNullInputCollections()
    {
        var sanitizer = new DefaultLogSanitizer();

        var result = sanitizer.Sanitize(new LogSanitizerRequest
        {
            Headers = null,
            QueryStringParameters = null,
            Body = null,
            IsBase64Encoded = false
        });

        Assert.Empty(result.Headers);
        Assert.Empty(result.QueryStringParameters);
        Assert.True(result.BodyLogged);
        Assert.Equal(string.Empty, result.Body);
    }
}
