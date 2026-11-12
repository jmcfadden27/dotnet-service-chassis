using Microsoft.AspNetCore.Mvc.Filters;

namespace QLS.Chassis.WebApi.Idempotency;

/// <summary>
/// Marks a controller or action as idempotent: a repeated request with the same idempotency key gets the stored
/// response instead of running again.
/// </summary>
/// <remarks>
/// Handled by <see cref="Middleware.HttpIdempotencyMiddleware"/>, which requires
/// <see cref="Extensions.IdempotencyServiceCollectionExtensions.AddChassisHttpIdempotency(Microsoft.Extensions.DependencyInjection.IServiceCollection, QLS.Chassis.Core.Idempotency.IHttpIdempotencyPolicy, HttpIdempotencyCleanupPolicy)"/>.
/// For minimal APIs, add it as endpoint metadata: <c>.WithMetadata(new HttpIdempotentAttribute())</c>.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class HttpIdempotentAttribute : Attribute, IFilterMetadata
{
}
