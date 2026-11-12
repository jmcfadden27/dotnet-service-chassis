using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using QLS.Chassis.Core.Logging;
using QLS.Chassis.WebApi.Auth;
using QLS.Chassis.WebApi.Exceptions;
using QLS.Chassis.WebApi.Observability;

namespace QLS.Chassis.WebApi.Extensions;

/// <summary>
/// Extension methods that register the core chassis web API services and JWT authentication.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the services the chassis middleware needs: the HTTP context accessor, health checks,
    /// <see cref="ICurrentUserAccessor"/> and <see cref="IUserContextService"/> (scoped), the structured log event factory,
    /// log sanitizer, <see cref="IWebApiRequestLogger"/> and <see cref="IStructuredAppLogger"/> (singletons),
    /// <see cref="GlobalExceptionHandler"/>, and problem details with the request's correlation ID added as
    /// <c>traceId</c>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddChassisWebApi(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddHealthChecks();
        services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
        services.AddScoped<IUserContextService, UserContextService>();
        services.AddSingleton<IStructuredLogEventFactory, DefaultStructuredLogEventFactory>();
        services.AddSingleton<ILogSanitizer, DefaultLogSanitizer>();
        services.AddSingleton<IWebApiRequestLogger, WebApiRequestLogger>();
        services.AddSingleton<IStructuredAppLogger, StructuredAppLogger>();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
            };
        });

        return services;
    }

    /// <summary>
    /// Registers JWT bearer authentication configured from <see cref="ChassisJwtOptions"/>, and an authorization fallback
    /// policy that requires an authenticated user on every endpoint without <c>[AllowAnonymous]</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The options are bound from <paramref name="sectionName"/> and validated at start-up by
    /// <see cref="ChassisJwtOptionsValidator"/>. Tokens must be signed, unexpired and from the configured issuer (a
    /// Cognito user pool ARN is turned into its issuer URL); the audience is checked only when set. Token validation,
    /// challenges and failures are logged through <see cref="IStructuredAppLogger"/>.
    /// </para>
    /// <para>
    /// Requires <see cref="AddChassisWebApi"/>. Call <c>UseAuthentication()</c> before
    /// <see cref="ApplicationBuilderExtensions.UseChassisWebApi"/>.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration to bind the options from.</param>
    /// <param name="sectionName">The configuration section to bind. Defaults to <c>Auth</c>.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddChassisJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "Auth")
    {
        services
            .AddOptions<ChassisJwtOptions>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<ChassisJwtOptions>, ChassisJwtOptionsValidator>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<ChassisJwtOptions>, IHostEnvironment, IStructuredAppLogger>((options, authOptionsAccessor, hostEnvironment, structuredAppLogger) =>
            {
                var authOptions = authOptionsAccessor.Value;

                var authority = CognitoIssuerResolver.Resolve(authOptions.Authority);
                var audience = authOptions.Audience?.Trim();
                var hasAudience = !string.IsNullOrWhiteSpace(audience);

                options.Authority = authority;
                options.RequireHttpsMetadata = authOptions.RequireHttpsMetadata;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidIssuers = [authority, authority.TrimEnd('/') + "/"],
                    ValidateAudience = hasAudience,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    RequireSignedTokens = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromMinutes(authOptions.ClockSkewMinutes)
                };

                options.Events = ChassisJwtBearerEvents.Create(structuredAppLogger);

                if (!hostEnvironment.IsDevelopment() && !hasAudience)
                    throw new InvalidOperationException("Auth:Audience is required outside Development environment.");
            });

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }
}
