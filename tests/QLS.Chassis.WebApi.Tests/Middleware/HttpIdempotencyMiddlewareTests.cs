using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using QLS.Chassis.Core.Idempotency;
using QLS.Chassis.WebApi.Idempotency;
using QLS.Chassis.WebApi.Middleware;

namespace QLS.Chassis.WebApi.Tests.Middleware;

public sealed class HttpIdempotencyMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_Bypasses_WhenEndpointIsNotIdempotent()
    {
        var nextCalled = false;
        var middleware = new HttpIdempotencyMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = CreateContext();
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "non-idempotent"));

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_Throws_WhenEndpointIsIdempotentAndCoordinatorMissing()
    {
        var middleware = new HttpIdempotencyMiddleware(_ => Task.CompletedTask);
        var context = CreateContext();
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(new HttpIdempotentAttribute()), "idempotent"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));

        Assert.Contains("AddChassisHttpIdempotency", ex.Message);
    }

    [Fact]
    public async Task InvokeAsync_WritesCachedResponse_WhenCoordinatorReturnsReplay()
    {
        var nextCalled = false;
        var middleware = new HttpIdempotencyMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = CreateContext(new RecordingCoordinator
        {
            OnExecute = (_, _) => Task.FromResult(new HttpIdempotencyExecutionResponse
            {
                StatusCode = 201,
                Body = "cached-response",
                Headers = new Dictionary<string, string> { ["X-From-Cache"] = "true" }
            })
        });

        context.Request.Method = HttpMethods.Put;
        context.Request.Path = "/users";
        context.Request.Headers["Idempotency-Key"] = "abc";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{}"));
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(new HttpIdempotentAttribute()), "idempotent"));

        await middleware.InvokeAsync(context);

        Assert.False(nextCalled);
        Assert.Equal(201, context.Response.StatusCode);
        Assert.Equal("true", context.Response.Headers["X-From-Cache"]);
    }

    [Fact]
    public async Task InvokeAsync_Replay_SkipsConnectionHeaders_AndWritesTheBody()
    {
        var middleware = new HttpIdempotencyMiddleware(_ => Task.CompletedTask);
        var context = CreateContext(new RecordingCoordinator
        {
            OnExecute = (_, _) => Task.FromResult(new HttpIdempotencyExecutionResponse
            {
                StatusCode = 201,
                Body = "{\"id\":\"o-1\"}",
                Headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = "application/json; charset=utf-8",
                    ["Location"] = "/orders/o-1",
                    ["Transfer-Encoding"] = "chunked",
                    ["Content-Length"] = "999",
                    ["Connection"] = "close",
                }
            })
        });

        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/orders";
        context.Request.Headers["Idempotency-Key"] = "abc";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{}"));
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(new HttpIdempotentAttribute()), "idempotent"));

        await middleware.InvokeAsync(context);

        Assert.Equal("/orders/o-1", context.Response.Headers["Location"]);
        Assert.Equal("application/json; charset=utf-8", context.Response.Headers["Content-Type"]);
        Assert.False(context.Response.Headers.ContainsKey("Transfer-Encoding"));
        Assert.False(context.Response.Headers.ContainsKey("Content-Length"));
        Assert.False(context.Response.Headers.ContainsKey("Connection"));
        Assert.Equal("{\"id\":\"o-1\"}", Encoding.UTF8.GetString(responseBody.ToArray()));
    }

    [Fact]
    public async Task InvokeAsync_ExecutesPipeline_WhenCoordinatorExecutesOperation()
    {
        var nextCalled = false;
        var middleware = new HttpIdempotencyMiddleware(async context =>
        {
            nextCalled = true;
            context.Response.StatusCode = 204;
            await context.Response.WriteAsync(string.Empty);
        });

        var coordinator = new RecordingCoordinator
        {
            OnExecute = (_, operation) => operation()
        };

        var context = CreateContext(coordinator);
        context.Request.Method = HttpMethods.Put;
        context.Request.Path = "/users";
        context.Request.Headers["Idempotency-Key"] = "abc";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{}"));
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(new HttpIdempotentAttribute()), "idempotent"));

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
        Assert.Equal(204, context.Response.StatusCode);
        Assert.NotNull(coordinator.LastRequest);
        Assert.Equal("PUT", coordinator.LastRequest!.HttpMethod);
    }

    private static DefaultHttpContext CreateContext(IHttpIdempotencyCoordinator? coordinator = null)
    {
        var services = new ServiceCollection();
        if (coordinator != null)
            services.AddSingleton(coordinator);

        var context = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString())], "test"))
        };

        context.Response.Body = new MemoryStream();
        return context;
    }

    private sealed class RecordingCoordinator : IHttpIdempotencyCoordinator
    {
        public HttpIdempotencyExecutionRequest? LastRequest { get; private set; }
        public Func<HttpIdempotencyExecutionRequest, Func<Task<HttpIdempotencyExecutionResponse>>, Task<HttpIdempotencyExecutionResponse>> OnExecute { get; init; }
            = (_, operation) => operation();

        public Task<HttpIdempotencyExecutionResponse> ExecuteAsync(HttpIdempotencyExecutionRequest request, Func<Task<HttpIdempotencyExecutionResponse>> operation)
        {
            LastRequest = request;
            return OnExecute(request, operation);
        }
    }
}
