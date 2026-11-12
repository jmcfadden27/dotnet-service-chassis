using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using QLS.Chassis.Core.Idempotency;
using QLS.Chassis.WebApi.Idempotency;
using QLS.Chassis.WebApi.Observability;

namespace QLS.Chassis.WebApi.Middleware;

/// <summary>
/// Middleware that makes endpoints marked with <see cref="HttpIdempotentAttribute"/> idempotent, through QLS.Chassis.Core's
/// <see cref="IHttpIdempotencyCoordinator"/>.
/// </summary>
/// <remarks>
/// <para>
/// Requests to other endpoints pass straight through. For a marked endpoint, the request's method, path, body, headers,
/// query string and user ID are passed to the coordinator, with an operation name of <c>METHOD:route-pattern</c>. When
/// the endpoint runs, its response is buffered in memory, copied to the client and handed to the coordinator to store;
/// when the coordinator returns a stored response instead, its status code, headers and body are written.
/// </para>
/// <para>
/// Requires <see cref="Extensions.IdempotencyServiceCollectionExtensions.AddChassisHttpIdempotency(Microsoft.Extensions.DependencyInjection.IServiceCollection, IHttpIdempotencyPolicy, HttpIdempotencyCleanupPolicy)"/>.
/// Run <c>UseAuthentication()</c> before it so records are scoped to the user. Added by
/// <see cref="Extensions.ApplicationBuilderExtensions.UseChassisWebApi"/>.
/// </para>
/// </remarks>
public sealed class HttpIdempotencyMiddleware
{
    private const string ExecutedThroughPipelineKey = "chassis.idempotency.executed";

    // Connection-level headers describe how the original response was sent, not the response itself. Replaying them
    // breaks the new response: a copied "Transfer-Encoding: chunked" stops the server from framing the body, so the
    // client gets no body. The server sets these itself for the replay.
    private static readonly HashSet<string> NotReplayedHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Connection",
        "Content-Length",
        "Keep-Alive",
        "Proxy-Connection",
        "TE",
        "Trailer",
        "Transfer-Encoding",
        "Upgrade",
    };

    private readonly RequestDelegate _next;

    /// <summary>
    /// Creates the middleware.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    public HttpIdempotencyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Runs the request, applying idempotency when the endpoint is marked with <see cref="HttpIdempotentAttribute"/>.
    /// </summary>
    /// <param name="context">The current request.</param>
    /// <returns>A task that completes when the response has been produced.</returns>
    /// <exception cref="InvalidOperationException">
    /// The endpoint is marked with <see cref="HttpIdempotentAttribute"/> but no <see cref="IHttpIdempotencyCoordinator"/>
    /// is registered.
    /// </exception>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsIdempotentEndpoint(context))
        {
            await _next(context);
            return;
        }

        var idempotencyCoordinator = context.RequestServices.GetService<IHttpIdempotencyCoordinator>();
        if (idempotencyCoordinator is null)
            throw new InvalidOperationException("Idempotency is enabled on endpoint but chassis idempotency services are not registered. Call AddChassisHttpIdempotency().");

        var request = new HttpIdempotencyExecutionRequest
        {
            OperationName = ResolveOperationName(context),
            HttpMethod = context.Request.Method,
            Path = context.Request.Path,
            Body = await ResolveRequestBodyAsync(context),
            Headers = ToHeadersDictionary(context.Request.Headers),
            QueryStringParameters = ToQueryDictionary(context.Request.Query),
            UserId = ResolveUserId(context),
        };

        var response = await idempotencyCoordinator.ExecuteAsync(request, () => ExecuteAndCaptureAsync(context));

        // The endpoint ran and its response has already gone to the client. Otherwise this is a replay: write the stored one.
        if (context.Items.ContainsKey(ExecutedThroughPipelineKey))
            return;

        await WriteCachedResponseAsync(context, response);
    }

    private async Task<HttpIdempotencyExecutionResponse> ExecuteAndCaptureAsync(HttpContext context)
    {
        var originalBody = context.Response.Body;

        // Capture the endpoint's response in memory, so it can be both stored for replays and sent to this client.
        await using var bufferedResponseBody = new MemoryStream();
        context.Response.Body = bufferedResponseBody;

        try
        {
            await _next(context);

            bufferedResponseBody.Position = 0;
            var responseBody = await new StreamReader(bufferedResponseBody).ReadToEndAsync();

            bufferedResponseBody.Position = 0;
            await bufferedResponseBody.CopyToAsync(originalBody);

            context.Items[ExecutedThroughPipelineKey] = true;

            return new HttpIdempotencyExecutionResponse
            {
                StatusCode = context.Response.StatusCode,
                Body = responseBody,
                Headers = ToResponseHeadersDictionary(context.Response.Headers)
            };
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    private static bool IsIdempotentEndpoint(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint == null)
            return false;

        return endpoint.Metadata.GetMetadata<HttpIdempotentAttribute>() is not null;
    }

    private static string ResolveOperationName(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        var routePattern = (endpoint as RouteEndpoint)?.RoutePattern.RawText;
        if (string.IsNullOrWhiteSpace(routePattern))
            routePattern = context.Request.Path;

        return $"{context.Request.Method}:{routePattern}";
    }

    private static string ResolveUserId(HttpContext context)
    {
        var user = context.User;
        if (user?.Identity?.IsAuthenticated != true)
            return string.Empty;

        return user.FindFirst("sub")?.Value
               ?? user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
               ?? user.FindFirst("cognito:username")?.Value
               ?? string.Empty;
    }

    private static IReadOnlyDictionary<string, string> ToHeadersDictionary(IHeaderDictionary headers)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in headers)
            values[header.Key] = header.Value.ToString();

        return values;
    }

    private static IReadOnlyDictionary<string, string> ToQueryDictionary(IQueryCollection query)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in query)
            values[pair.Key] = pair.Value.ToString();

        return values;
    }

    private static Dictionary<string, string>? ToResponseHeadersDictionary(IHeaderDictionary headers)
    {
        if (headers.Count == 0)
            return null;

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in headers)
            values[header.Key] = header.Value.ToString();

        return values;
    }

    private static async Task<string> ResolveRequestBodyAsync(HttpContext context)
    {
        if (context.Items.TryGetValue(RequestLoggingConstants.RequestBodyItemKey, out var requestBodyObj)
            && requestBodyObj is string bodyFromLogging)
        {
            return bodyFromLogging;
        }

        if (!context.Request.Body.CanRead || context.Request.Body == Stream.Null)
            return string.Empty;

        context.Request.EnableBuffering();
        if (context.Request.Body.CanSeek)
            context.Request.Body.Position = 0;

        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var body = await reader.ReadToEndAsync();

        if (context.Request.Body.CanSeek)
            context.Request.Body.Position = 0;

        return body;
    }

    private static async Task WriteCachedResponseAsync(HttpContext context, HttpIdempotencyExecutionResponse response)
    {
        context.Response.StatusCode = response.StatusCode;

        if (response.Headers != null)
        {
            foreach (var header in response.Headers)
            {
                if (!NotReplayedHeaders.Contains(header.Key))
                    context.Response.Headers[header.Key] = header.Value;
            }
        }

        if (!string.IsNullOrEmpty(response.Body))
            await context.Response.WriteAsync(response.Body);
    }
}
