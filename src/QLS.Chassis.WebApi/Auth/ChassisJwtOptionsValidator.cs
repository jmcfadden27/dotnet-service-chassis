using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace QLS.Chassis.WebApi.Auth;

/// <summary>
/// Validates <see cref="ChassisJwtOptions"/> at start-up, so a misconfigured issuer stops the app instead of failing
/// on the first request.
/// </summary>
public sealed class ChassisJwtOptionsValidator : IValidateOptions<ChassisJwtOptions>
{
    private readonly IHostEnvironment _hostEnvironment;

    /// <summary>
    /// Creates the validator.
    /// </summary>
    /// <param name="hostEnvironment">The host environment, used to relax the audience requirement in Development.</param>
    public ChassisJwtOptionsValidator(IHostEnvironment hostEnvironment)
    {
        _hostEnvironment = hostEnvironment;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Fails when <see cref="ChassisJwtOptions.Authority"/> is missing or does not resolve to an absolute URI, when
    /// <see cref="ChassisJwtOptions.ClockSkewMinutes"/> is outside 0 to 10, or when
    /// <see cref="ChassisJwtOptions.Audience"/> is missing outside the Development environment.
    /// </remarks>
    public ValidateOptionsResult Validate(string? name, ChassisJwtOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Authority))
            return ValidateOptionsResult.Fail("Auth:Authority is required.");

        var resolvedAuthority = CognitoIssuerResolver.Resolve(options.Authority);
        if (!Uri.TryCreate(resolvedAuthority, UriKind.Absolute, out _))
            return ValidateOptionsResult.Fail("Auth:Authority must be a valid absolute URI or Cognito User Pool ARN.");

        if (options.ClockSkewMinutes < 0 || options.ClockSkewMinutes > 10)
            return ValidateOptionsResult.Fail("Auth:ClockSkewMinutes must be between 0 and 10.");

        if (!_hostEnvironment.IsDevelopment() && string.IsNullOrWhiteSpace(options.Audience))
            return ValidateOptionsResult.Fail("Auth:Audience is required outside Development environment.");

        return ValidateOptionsResult.Success;
    }
}
