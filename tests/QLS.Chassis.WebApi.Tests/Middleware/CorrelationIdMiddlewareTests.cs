using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QLS.Chassis.WebApi.Middleware;

namespace QLS.Chassis.WebApi.Tests.Middleware;

public sealed class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_UsesIncomingHeader_WhenPresent()
    {
        var context = CreateContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "incoming-correlation";
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal("incoming-correlation", context.TraceIdentifier);
        Assert.Equal("incoming-correlation", context.Response.Headers[CorrelationIdMiddleware.HeaderName]);
    }

    [Fact]
    public async Task InvokeAsync_GeneratesCorrelationId_WhenMissing()
    {
        var context = CreateContext();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.False(string.IsNullOrWhiteSpace(context.TraceIdentifier));
        Assert.Equal(context.TraceIdentifier, context.Response.Headers[CorrelationIdMiddleware.HeaderName]);
    }

    private static DefaultHttpContext CreateContext()
    {
        var services = new ServiceCollection()
            .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug))
            .BuildServiceProvider();

        return new DefaultHttpContext { RequestServices = services };
    }
}
