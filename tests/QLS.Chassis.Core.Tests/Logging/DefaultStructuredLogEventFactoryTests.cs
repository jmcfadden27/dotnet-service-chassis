using QLS.Chassis.Core.Logging;

namespace QLS.Chassis.Core.Tests.Logging;

public sealed class DefaultStructuredLogEventFactoryTests
{
    [Fact]
    public void CreateRequestSucceeded_SetsStandardFields()
    {
        var factory = new DefaultStructuredLogEventFactory();

        var evt = factory.CreateRequestSucceeded("svc", "req-1", "corr-1", "trace-1", "user-1", "GET", "/orders", 200);

        Assert.Equal(1, evt.SchemaVersion);
        Assert.Equal("Information", evt.Level);
        Assert.Equal(LogEventNames.RequestSucceeded, evt.EventName);
        Assert.Equal("svc", evt.Service);
        Assert.Equal("corr-1", evt.CorrelationId);
        Assert.Equal("trace-1", evt.TraceId);
        Assert.Equal("req-1", evt.RequestId);
        Assert.Equal(200, evt.StatusCode);
    }

    [Fact]
    public void CreateOperationFailed_ClassifiesConflict()
    {
        var factory = new DefaultStructuredLogEventFactory();

        var evt = factory.CreateOperationFailed(
            "svc",
            "req-2",
            "orders.update",
            "corr-2",
            "trace-2",
            "user-2",
            409,
            55,
            new Dictionary<string, string?> { ["orderId"] = "o-1" },
            "msg-1",
            "msg-0",
            new InvalidOperationException("bad"));

        Assert.Equal(LogEventNames.OperationFailed, evt.EventName);
        Assert.Equal("Error", evt.Level);
        Assert.Equal("conflict", evt.ErrorCategory);
        Assert.Equal("msg-1", evt.MessageId);
        Assert.Equal("msg-0", evt.CausationId);
        Assert.Equal(55, evt.DurationMs);
        Assert.NotNull(evt.Metadata);
        Assert.Equal("o-1", evt.Metadata!["orderId"]);
    }

    [Theory]
    [InlineData(401, "authentication")]
    [InlineData(403, "authorization")]
    [InlineData(404, "not_found")]
    [InlineData(429, "rate_limited")]
    [InlineData(422, "validation")]
    [InlineData(500, "dependency_failure")]
    [InlineData(200, "unexpected")]
    public void CreateRequestFailed_ClassifiesKnownStatusCodes(int statusCode, string expectedCategory)
    {
        var factory = new DefaultStructuredLogEventFactory();

        var evt = factory.CreateRequestFailed(
            "svc",
            "req-3",
            "corr-3",
            "trace-3",
            "user-3",
            null,
            null,
            statusCode,
            new InvalidOperationException("boom"));

        Assert.Equal(expectedCategory, evt.ErrorCategory);
        Assert.Equal("UNKNOWN /", evt.Operation);
    }

    [Fact]
    public void CreateRequestFailed_UsesIdempotencyCategoryAndTruncatesStackTrace()
    {
        var factory = new DefaultStructuredLogEventFactory();
        var longMessage = new string('x', 20000);
        var metadata = new Dictionary<string, object?>
        {
            ["path"] = "/override",
            ["custom"] = 123,
        };

        var evt = factory.CreateRequestFailed(
            "svc",
            "req-4",
            "corr-4",
            "trace-4",
            "user-4",
            "POST",
            "/orders",
            500,
            new FakeIdempotencyFailureException(longMessage),
            metadata);

        Assert.Equal("idempotency", evt.ErrorCategory);
        Assert.NotNull(evt.ErrorStackTrace);
        Assert.True(evt.ErrorStackTrace!.Length <= 16 * 1024);
        Assert.NotNull(evt.Metadata);
        Assert.Equal("/override", evt.Metadata!["path"]);
        Assert.Equal(123, evt.Metadata["custom"]);
    }

    [Fact]
    public void CreateOperationSucceeded_ConvertsMetadataToCaseInsensitiveDictionary()
    {
        var factory = new DefaultStructuredLogEventFactory();

        var evt = factory.CreateOperationSucceeded(
            "svc",
            "req-5",
            "orders.create",
            "corr-5",
            "trace-5",
            "user-5",
            201,
            25,
            new Dictionary<string, string?> { ["OrderId"] = "O-1" },
            "msg-5",
            null);

        Assert.NotNull(evt.Metadata);
        Assert.Equal("O-1", evt.Metadata!["orderid"]);
    }

    private sealed class FakeIdempotencyFailureException : Exception
    {
        public FakeIdempotencyFailureException(string message) : base(message)
        {
        }
    }
}
