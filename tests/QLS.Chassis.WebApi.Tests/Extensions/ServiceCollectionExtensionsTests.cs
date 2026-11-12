using Microsoft.Extensions.DependencyInjection;
using QLS.Chassis.Core.Logging;
using QLS.Chassis.WebApi.Auth;
using QLS.Chassis.WebApi.Extensions;
using QLS.Chassis.WebApi.Observability;

namespace QLS.Chassis.WebApi.Tests.Extensions;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddChassisWebApi_RegistersCoreServices()
    {
        var services = new ServiceCollection();

        services.AddChassisWebApi();

        Assert.Contains(services, d => d.ServiceType == typeof(ICurrentUserAccessor));
        Assert.Contains(services, d => d.ServiceType == typeof(IUserContextService));
        Assert.Contains(services, d => d.ServiceType == typeof(IStructuredLogEventFactory));
        Assert.Contains(services, d => d.ServiceType == typeof(ILogSanitizer));
        Assert.Contains(services, d => d.ServiceType == typeof(IWebApiRequestLogger));
        Assert.Contains(services, d => d.ServiceType == typeof(IStructuredAppLogger));
    }
}
