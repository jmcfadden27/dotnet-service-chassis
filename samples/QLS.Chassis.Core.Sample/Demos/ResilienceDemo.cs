using QLS.Chassis.Core.Resilience;

namespace QLS.Chassis.Core.Sample.Demos;

/// <summary>Retries a failing dependency, then opens its circuit so later calls fail fast.</summary>
internal static class ResilienceDemo
{
    public static async Task RunAsync(IResilienceExecutor resilience)
    {
        Section.Title("Resilience");

        // A dependency that recovers: the first attempt fails, the retry succeeds.
        var attempts = 0;
        var flaky = new ResilienceOptions { DependencyName = "inventory-api", MaxRetries = 2, BaseDelayMs = 50, TimeoutMs = 500 };
        var stock = await resilience.ExecuteAsync(flaky, _ =>
        {
            attempts++;
            return attempts == 1 ? throw new HttpRequestException("blip") : Task.FromResult(17);
        });
        Console.WriteLine($"inventory-api answered {stock} after {attempts} attempts");

        // A dependency that stays down: retries run out, and with a small sample size the circuit opens.
        var down = new ResilienceOptions
        {
            DependencyName = "payments-api",
            MaxRetries = 2,
            BaseDelayMs = 50,
            TimeoutMs = 500,
            CircuitMinThroughput = 3,
            CircuitFailureRatio = 0.5,
            CircuitBreakSeconds = 30,
        };
        await Try(() => resilience.ExecuteAsync<string>(down, _ => throw new HttpRequestException("payments-api is down")));

        // The next call fails at once without reaching the dependency.
        await Try(() => resilience.ExecuteAsync(down, _ => Task.FromResult("never called")));
    }

    private static async Task Try(Func<Task> call)
    {
        try
        {
            await call();
        }
        catch (ResilienceExecutionException ex)
        {
            Console.WriteLine($"Gave up on {ex.DependencyName}: {ex.FailureType}, retry after {ex.RetryAfterSeconds}s");
        }
    }
}
