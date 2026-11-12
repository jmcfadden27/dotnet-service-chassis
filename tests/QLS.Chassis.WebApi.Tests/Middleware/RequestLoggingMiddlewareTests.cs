using System.Text;
using Microsoft.AspNetCore.Http;
using QLS.Chassis.WebApi.Middleware;
using QLS.Chassis.WebApi.Observability;

namespace QLS.Chassis.WebApi.Tests.Middleware;

public sealed class RequestLoggingMiddlewareTests
{
    private const string RequestBodyItemKey = "chassis.request.body";

    [Fact]
    public async Task InvokeAsync_CapturesRequestBody_ForPostAndResetsPosition()
    {
        var logger = new RecordingRequestLogger();
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"x\":1}"));

        var middleware = new RequestLoggingMiddleware(async httpContext =>
        {
            using var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            Assert.Equal("{\"x\":1}", body);
        });

        await middleware.InvokeAsync(context, logger);

        Assert.Equal("{\"x\":1}", context.Items[RequestBodyItemKey]);
        Assert.Equal(1, logger.SuccessCount);
    }

    [Fact]
    public async Task InvokeAsync_DoesNotCaptureBody_ForGet()
    {
        var logger = new RecordingRequestLogger();
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("ignored"));

        var middleware = new RequestLoggingMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, logger);

        Assert.Null(context.Items[RequestBodyItemKey]);
        Assert.Equal(1, logger.SuccessCount);
    }

    private sealed class RecordingRequestLogger : IWebApiRequestLogger
    {
        public int SuccessCount { get; private set; }

        public void LogSuccess(HttpContext context)
        {
            SuccessCount++;
        }

        public void LogError(HttpContext context, Exception exception, int statusCode)
        {
        }
    }
}
