using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace QLS.Chassis.WebApi.Extensions;

/// <summary>
/// Extension methods that add the chassis JWT security scheme to Swagger.
/// </summary>
public static class SwaggerGenOptionsExtensions
{
    private const string BearerSchemeName = "Bearer";

    /// <summary>
    /// Adds a <c>Bearer</c> HTTP security scheme (the token is entered without the <c>Bearer </c> prefix) and marks
    /// operations whose action or controller has <c>[Authorize]</c> as needing it, with 401 and 403 responses.
    /// Operations with <c>[AllowAnonymous]</c> are left open.
    /// </summary>
    /// <remarks>
    /// Endpoints that rely only on the fallback authorization policy are not marked, so put <c>[Authorize]</c> on
    /// controllers to keep the documentation accurate.
    /// </remarks>
    /// <param name="options">The Swagger generator options.</param>
    /// <returns>The same options, for chaining.</returns>
    public static SwaggerGenOptions AddChassisJwtSecurity(this SwaggerGenOptions options)
    {
        options.AddSecurityDefinition(BearerSchemeName, new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Description = "Enter JWT Bearer token only (without 'Bearer ' prefix).",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });

        options.OperationFilter<AuthorizeOperationFilter>();

        return options;
    }

    private sealed class AuthorizeOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (HasAllowAnonymous(context))
                return;

            if (!HasAuthorize(context))
                return;

            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = BearerSchemeName
                    }
                }] = []
            });

            operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });
            operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden" });
        }

        private static bool HasAllowAnonymous(OperationFilterContext context)
        {
            var method = context.MethodInfo;
            var controller = method.DeclaringType;

            return method.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any()
                   || (controller?.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any() ?? false);
        }

        private static bool HasAuthorize(OperationFilterContext context)
        {
            var method = context.MethodInfo;
            var controller = method.DeclaringType;

            return method.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any()
                   || (controller?.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() ?? false);
        }
    }
}
