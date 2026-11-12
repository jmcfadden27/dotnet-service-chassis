using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QLS.Chassis.Core.Resilience;
using QLS.Chassis.WebApi.Data;
using QLS.Chassis.WebApi.Extensions;
using QLS.Chassis.WebApi.Tests.TestDoubles;

namespace QLS.Chassis.WebApi.Tests.Extensions;

public sealed class DatabaseServiceCollectionExtensionsTests
{
    [Fact]
    public void AddChassisPostgresDatabaseResiliency_RegistersOptionsAndExecutors()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:AppDb"] = "Host=localhost;Port=5432;Database=app;Username=app"
            })
            .Build();

        var services = new ServiceCollection();

        services.AddChassisPostgresDatabaseResiliency(configuration, "AppDb", DatabaseResiliencePolicies.Default);

        Assert.Contains(services, d => d.ServiceType == typeof(ChassisDatabaseOptions));
        Assert.Contains(services, d => d.ServiceType == typeof(IDatabaseResilienceExecutor) && d.ImplementationType == typeof(CoreResilienceDatabaseExecutor));
        Assert.Contains(services, d => d.ServiceType == typeof(IResilienceExecutor) && d.ImplementationType == typeof(ResilienceExecutor));

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<ChassisDatabaseOptions>();
        Assert.Contains("Database=app", options.ConnectionString);
    }

    [Fact]
    public async Task AddChassisPostgresDatabaseResiliency_ResilienceEvents_GoToTheLogger_WithTheApplicationName()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:AppDb"] = "Host=localhost;Database=unused" })
            .Build();
        var loggerProvider = new RecordingLoggerProvider();
        var services = new ServiceCollection();
        services.AddHttpContextAccessor();
        services.AddLogging(logging => logging.AddProvider(loggerProvider));
        services.AddSingleton<IHostEnvironment>(new HostEnvironmentStub { ApplicationName = "orders-api" });

        services.AddChassisPostgresDatabaseResiliency(configuration, "AppDb", new ResilienceOptions
        {
            DependencyName = "postgres",
            MaxRetries = 1,
            BaseDelayMs = 1,
            TimeoutMs = 1000,
        });

        using var provider = services.BuildServiceProvider();
        var executor = provider.GetRequiredService<IResilienceExecutor>();
        var policy = provider.GetRequiredService<ResilienceOptions>();

        await Assert.ThrowsAsync<ResilienceExecutionException>(() =>
            executor.ExecuteAsync<int>(policy, _ => throw new TimeoutException("db timeout")));

        var events = loggerProvider.Messages
            .Where(m => m.Category == typeof(ResilienceExecutor).FullName)
            .Select(m => JsonDocument.Parse(m.Message).RootElement)
            .ToList();
        Assert.Contains(events, e => e.GetProperty("EventName").GetString() == "retry");
        Assert.All(events, e => Assert.Equal("orders-api", e.GetProperty("Service").GetString()));
    }

    private sealed class RecordingLoggerProvider : ILoggerProvider
    {
        private readonly List<(string Category, string Message)> _messages = new();

        public IReadOnlyList<(string Category, string Message)> Messages
        {
            get
            {
                lock (_messages)
                    return _messages.ToList();
            }
        }

        public ILogger CreateLogger(string categoryName) => new RecordingLogger(categoryName, _messages);

        public void Dispose()
        {
        }

        private sealed class RecordingLogger(string category, List<(string Category, string Message)> messages) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                lock (messages)
                    messages.Add((category, formatter(state, exception)));
            }
        }
    }
}
