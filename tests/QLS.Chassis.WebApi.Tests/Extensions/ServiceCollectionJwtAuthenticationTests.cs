using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using QLS.Chassis.WebApi.Extensions;
using QLS.Chassis.WebApi.Observability;
using QLS.Chassis.WebApi.Tests.TestDoubles;

namespace QLS.Chassis.WebApi.Tests.Extensions;

public sealed class ServiceCollectionJwtAuthenticationTests
{
    [Fact]
    public void AddChassisJwtAuthentication_ConfiguresJwtBearerOptions()
    {
        var services = new ServiceCollection();
        services.AddSingleton<Microsoft.Extensions.Hosting.IHostEnvironment>(new HostEnvironmentStub { EnvironmentName = "Development" });
        services.AddSingleton<IStructuredAppLogger, NoopStructuredAppLogger>();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:Authority"] = "arn:aws:cognito-idp:us-east-1:123456789012:userpool/us-east-1_Example",
                ["Auth:Audience"] = "test-audience",
                ["Auth:RequireHttpsMetadata"] = "true",
                ["Auth:ClockSkewMinutes"] = "2",
            })
            .Build();

        services.AddChassisJwtAuthentication(configuration);

        using var provider = services.BuildServiceProvider();
        var monitor = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        var options = monitor.Get(JwtBearerDefaults.AuthenticationScheme);

        Assert.Equal("https://cognito-idp.us-east-1.amazonaws.com/us-east-1_Example", options.Authority);
        Assert.NotNull(options.Events);
        Assert.True(options.TokenValidationParameters.ValidateAudience);
        Assert.Equal("test-audience", options.TokenValidationParameters.ValidAudience);
    }

    [Fact]
    public void AddChassisJwtAuthentication_ThrowsOutsideDevelopmentWhenAudienceMissing()
    {
        var services = new ServiceCollection();
        services.AddSingleton<Microsoft.Extensions.Hosting.IHostEnvironment>(new HostEnvironmentStub { EnvironmentName = "Production" });
        services.AddSingleton<IStructuredAppLogger, NoopStructuredAppLogger>();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:Authority"] = "https://issuer.example.com",
                ["Auth:ClockSkewMinutes"] = "2",
            })
            .Build();

        services.AddChassisJwtAuthentication(configuration);

        using var provider = services.BuildServiceProvider();
        var monitor = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();

        Assert.Throws<OptionsValidationException>(() => monitor.Get(JwtBearerDefaults.AuthenticationScheme));
    }

    private sealed class NoopStructuredAppLogger : IStructuredAppLogger
    {
        public void LogOperationSucceeded(string operation, int statusCode, long? durationMs = null, IReadOnlyDictionary<string, string?>? operationMetadata = null, string? messageId = null, string? causationId = null)
        {
        }

        public void LogOperationFailed(string operation, Exception exception, int statusCode, long? durationMs = null, IReadOnlyDictionary<string, string?>? operationMetadata = null, string? messageId = null, string? causationId = null)
        {
        }

        public void LogOperationWarning(string operation, Exception exception, int statusCode, long? durationMs = null, IReadOnlyDictionary<string, string?>? operationMetadata = null, string? messageId = null, string? causationId = null)
        {
        }
    }
}
