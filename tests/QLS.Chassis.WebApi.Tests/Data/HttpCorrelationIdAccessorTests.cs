using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using QLS.Chassis.WebApi.Data;

namespace QLS.Chassis.WebApi.Tests.Data;

public sealed class HttpCorrelationIdAccessorTests
{
    [Fact]
    public void CorrelationId_ReturnsHttpTraceIdentifier_WhenAvailable()
    {
        var context = new DefaultHttpContext { TraceIdentifier = "trace-http" };
        var accessor = new HttpCorrelationIdAccessor(new HttpContextAccessor { HttpContext = context });

        Assert.Equal("trace-http", accessor.CorrelationId);
    }

    [Fact]
    public void CorrelationId_FallsBackToActivityTraceId()
    {
        using var activity = new Activity("test");
        activity.Start();

        var accessor = new HttpCorrelationIdAccessor(new HttpContextAccessor());

        Assert.Equal(activity.TraceId.ToString(), accessor.CorrelationId);
    }
}
