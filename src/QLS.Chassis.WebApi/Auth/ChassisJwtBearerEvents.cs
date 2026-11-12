using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using QLS.Chassis.WebApi.Observability;

namespace QLS.Chassis.WebApi.Auth;

internal static class ChassisJwtBearerEvents
{
    internal static JwtBearerEvents Create(IStructuredAppLogger logger)
    {
        return new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                logger.LogOperationWarning(
                    operation: "jwt_authentication_failed",
                    exception: context.Exception,
                    statusCode: StatusCodes.Status401Unauthorized,
                    operationMetadata: new Dictionary<string, string?>
                    {
                        ["path"] = context.Request.Path,
                        ["scheme"] = context.Scheme.Name,
                    });
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                logger.LogOperationSucceeded(
                    operation: "jwt_challenge_issued",
                    statusCode: StatusCodes.Status401Unauthorized,
                    operationMetadata: new Dictionary<string, string?>
                    {
                        ["path"] = context.Request.Path,
                        ["scheme"] = context.Scheme.Name,
                        ["error"] = context.Error,
                        ["errorDescription"] = context.ErrorDescription,
                    });
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var subject = context.Principal?.FindFirstValue("sub")
                              ?? context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? string.Empty;

                logger.LogOperationSucceeded(
                    operation: "jwt_token_validated",
                    statusCode: StatusCodes.Status200OK,
                    operationMetadata: new Dictionary<string, string?>
                    {
                        ["path"] = context.Request.Path,
                        ["scheme"] = context.Scheme.Name,
                        ["subject"] = subject,
                    });
                return Task.CompletedTask;
            }
        };
    }
}
