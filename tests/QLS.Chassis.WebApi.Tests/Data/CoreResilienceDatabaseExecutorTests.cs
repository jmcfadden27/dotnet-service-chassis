using Moq;
using QLS.Chassis.Core.Resilience;
using QLS.Chassis.WebApi.Data;

namespace QLS.Chassis.WebApi.Tests.Data;

public sealed class CoreResilienceDatabaseExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsValue_WhenUnderlyingExecutorSucceeds()
    {
        var resilience = new Mock<IResilienceExecutor>();
        var options = new ResilienceOptions { DependencyName = "postgres" };
        resilience
            .Setup(x => x.ExecuteAsync(options, It.IsAny<Func<CancellationToken, Task<int>>>(), It.IsAny<CancellationToken>()))
            .Returns(async (ResilienceOptions _, Func<CancellationToken, Task<int>> op, CancellationToken ct) => await op(ct));

        var sut = new CoreResilienceDatabaseExecutor(resilience.Object, options);

        var result = await sut.ExecuteAsync(_ => Task.FromResult(42), CancellationToken.None);

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task ExecuteAsync_MapsCircuitOpenToDatabaseCircuitOpenException()
    {
        var resilience = new Mock<IResilienceExecutor>();
        var options = new ResilienceOptions { DependencyName = "postgres", RetryAfterSeconds = 12 };
        resilience
            .Setup(x => x.ExecuteAsync(options, It.IsAny<Func<CancellationToken, Task<int>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ResilienceExecutionException("postgres", ResilienceFailureType.CircuitOpen, "open", retryAfterSeconds: 12));

        var sut = new CoreResilienceDatabaseExecutor(resilience.Object, options);

        var ex = await Assert.ThrowsAsync<DatabaseCircuitOpenException>(() =>
            sut.ExecuteAsync(_ => Task.FromResult(1), CancellationToken.None));

        Assert.True(ex.RetryAfterUtc > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task ExecuteAsync_NonGeneric_InvokesOperation()
    {
        var resilience = new Mock<IResilienceExecutor>();
        var options = new ResilienceOptions { DependencyName = "postgres" };
        resilience
            .Setup(x => x.ExecuteAsync(options, It.IsAny<Func<CancellationToken, Task<bool>>>(), It.IsAny<CancellationToken>()))
            .Returns(async (ResilienceOptions _, Func<CancellationToken, Task<bool>> op, CancellationToken ct) => await op(ct));

        var sut = new CoreResilienceDatabaseExecutor(resilience.Object, options);
        var called = false;

        await sut.ExecuteAsync(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }, CancellationToken.None);

        Assert.True(called);
    }
}
