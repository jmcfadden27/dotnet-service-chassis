using Moq;
using QLS.Chassis.Core.Idempotency;

namespace QLS.Chassis.Core.Tests.Idempotency;

public sealed class IdempotencyCoordinatorTests
{
    [Fact]
    public async Task ExecuteAsync_Throws_WhenHeaderMissing()
    {
        var coordinator = CreateCoordinator();
        var request = new HttpIdempotencyExecutionRequest
        {
            OperationName = "orders.create",
            HttpMethod = "POST",
            Path = "/orders",
            Headers = new Dictionary<string, string>()
        };

        await Assert.ThrowsAsync<IdempotencyKeyRequiredException>(() =>
            coordinator.ExecuteAsync(request, () => Task.FromResult(new HttpIdempotencyExecutionResponse { StatusCode = 200 })));
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsCachedResponse_WhenCompletedExists()
    {
        var store = new Mock<IIdempotencyStore>();
        store.Setup(s => s.TryClaimAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<long>())).ReturnsAsync(false);
        store.Setup(s => s.LoadAsync(It.IsAny<string>())).ReturnsAsync(new IdempotencyStoreRecord
        {
            Status = "COMPLETED",
            Fingerprint = Fingerprint("orders.create|POST|/orders||{}"),
            ResponseJson = "{\"statusCode\":201,\"body\":\"cached\"}"
        });

        var coordinator = CreateCoordinator(store.Object);
        var request = new HttpIdempotencyExecutionRequest
        {
            OperationName = "orders.create",
            HttpMethod = "POST",
            Path = "/orders",
            Body = "{}",
            Headers = new Dictionary<string, string> { ["Idempotency-Key"] = "k1" }
        };

        var response = await coordinator.ExecuteAsync(request, () => Task.FromResult(new HttpIdempotencyExecutionResponse { StatusCode = 500 }));
        Assert.Equal(201, response.StatusCode);
        Assert.Equal("cached", response.Body);
    }

    [Fact]
    public async Task ExecuteAsync_StoresCompletedResponse_OnSuccess()
    {
        var store = new Mock<IIdempotencyStore>();
        store.Setup(s => s.TryClaimAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<long>())).ReturnsAsync(true);
        store.Setup(s => s.SaveCompletedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HttpIdempotencyExecutionResponse>(), It.IsAny<long>())).Returns(Task.CompletedTask);

        var coordinator = CreateCoordinator(store.Object);
        var request = new HttpIdempotencyExecutionRequest
        {
            OperationName = "orders.create",
            HttpMethod = "POST",
            Path = "/orders",
            Body = "{}",
            Headers = new Dictionary<string, string> { ["Idempotency-Key"] = "k1" }
        };

        var response = await coordinator.ExecuteAsync(request, () => Task.FromResult(new HttpIdempotencyExecutionResponse { StatusCode = 201, Body = "ok" }));
        Assert.Equal(201, response.StatusCode);
        store.Verify(s => s.SaveCompletedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HttpIdempotencyExecutionResponse>(), It.IsAny<long>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotPersist_ForServerError()
    {
        var store = new Mock<IIdempotencyStore>();
        store.Setup(s => s.TryClaimAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<long>())).ReturnsAsync(true);
        store.Setup(s => s.DeleteAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        var coordinator = CreateCoordinator(store.Object);
        var request = new HttpIdempotencyExecutionRequest
        {
            OperationName = "orders.create",
            HttpMethod = "POST",
            Path = "/orders",
            Body = "{}",
            Headers = new Dictionary<string, string> { ["Idempotency-Key"] = "k1" }
        };

        var response = await coordinator.ExecuteAsync(request, () => Task.FromResult(new HttpIdempotencyExecutionResponse { StatusCode = 500 }));
        Assert.Equal(500, response.StatusCode);
        store.Verify(s => s.DeleteAsync(It.IsAny<string>()), Times.Once);
        store.Verify(s => s.SaveCompletedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<HttpIdempotencyExecutionResponse>(), It.IsAny<long>()), Times.Never);
    }

    private static IHttpIdempotencyCoordinator CreateCoordinator(IIdempotencyStore? store = null)
    {
        return new HttpIdempotencyCoordinator(store ?? Mock.Of<IIdempotencyStore>(), new DefaultHttpIdempotencyPolicy());
    }

    private static string Fingerprint(string value)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
