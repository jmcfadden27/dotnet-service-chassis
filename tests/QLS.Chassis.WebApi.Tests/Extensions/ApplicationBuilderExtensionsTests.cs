using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.TestHost;
using QLS.Chassis.WebApi.Extensions;

namespace QLS.Chassis.WebApi.Tests.Extensions;

public sealed class ApplicationBuilderExtensionsTests
{
    [Fact]
    public async Task MapChassisHealthEndpoints_MapsLiveAndReadyAsAnonymous()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddHealthChecks();

        var app = builder.Build();
        app.MapChassisHealthEndpoints();

        await app.StartAsync();
        var client = app.GetTestClient();
        var live = await client.GetAsync("/health/live");
        var ready = await client.GetAsync("/health/ready");

        Assert.Equal(200, (int)live.StatusCode);
        Assert.Equal(200, (int)ready.StatusCode);
    }

    [Fact]
    public async Task UseChassisWebApi_InvokesMiddlewarePipeline()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddLogging();
        builder.Services.AddChassisWebApi();

        var app = builder.Build();
        app.UseChassisWebApi();
        app.MapGet("/ping", context =>
        {
            context.Response.StatusCode = 204;
            return Task.CompletedTask;
        });

        await app.StartAsync();
        var client = app.GetTestClient();
        var response = await client.GetAsync("/ping");

        Assert.Equal(204, (int)response.StatusCode);
        Assert.True(response.Headers.Contains("X-Correlation-Id"));
    }
}
