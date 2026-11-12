using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace QLS.Chassis.WebApi.Tests.TestDoubles;

internal sealed class HostEnvironmentStub : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Production;
    public string ApplicationName { get; set; } = "QLS.Chassis.WebApi.Tests";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
