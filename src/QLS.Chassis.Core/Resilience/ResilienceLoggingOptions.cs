namespace QLS.Chassis.Core.Resilience;

/// <summary>
/// How <see cref="ResilienceExecutor"/> identifies the service in its log events.
/// </summary>
public sealed class ResilienceLoggingOptions
{
    /// <summary>
    /// The service name written to each event's <c>Service</c> field. When blank, the entry assembly's name is used,
    /// which is also ASP.NET Core's default application name.
    /// </summary>
    public string? ServiceName { get; init; }
}
