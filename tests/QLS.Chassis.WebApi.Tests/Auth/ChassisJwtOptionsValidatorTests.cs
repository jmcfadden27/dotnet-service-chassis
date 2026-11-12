using Microsoft.Extensions.Options;
using QLS.Chassis.WebApi.Auth;
using QLS.Chassis.WebApi.Tests.TestDoubles;

namespace QLS.Chassis.WebApi.Tests.Auth;

public sealed class ChassisJwtOptionsValidatorTests
{
    [Fact]
    public void Validate_Fails_WhenAuthorityMissing()
    {
        var validator = CreateValidator("Development");

        var result = validator.Validate(null, new ChassisJwtOptions());

        Assert.True(result.Failed);
        Assert.Contains("Authority", result.FailureMessage);
    }

    [Fact]
    public void Validate_Fails_WhenClockSkewOutOfRange()
    {
        var validator = CreateValidator("Development");

        var result = validator.Validate(null, new ChassisJwtOptions
        {
            Authority = "https://issuer.example.com",
            ClockSkewMinutes = 42,
        });

        Assert.True(result.Failed);
        Assert.Contains("ClockSkewMinutes", result.FailureMessage);
    }

    [Fact]
    public void Validate_Fails_WhenAudienceMissingOutsideDevelopment()
    {
        var validator = CreateValidator("Production");

        var result = validator.Validate(null, new ChassisJwtOptions
        {
            Authority = "https://issuer.example.com",
            Audience = "",
            ClockSkewMinutes = 2,
        });

        Assert.True(result.Failed);
        Assert.Contains("Audience", result.FailureMessage);
    }

    [Fact]
    public void Validate_Succeeds_InDevelopmentWithoutAudience()
    {
        var validator = CreateValidator("Development");

        var result = validator.Validate(null, new ChassisJwtOptions
        {
            Authority = "https://issuer.example.com",
            Audience = "",
            ClockSkewMinutes = 2,
        });

        Assert.Equal(ValidateOptionsResult.Success, result);
    }

    [Fact]
    public void Validate_Succeeds_ForCognitoArnAuthority()
    {
        var validator = CreateValidator("Development");

        var result = validator.Validate(null, new ChassisJwtOptions
        {
            Authority = "arn:aws:cognito-idp:us-east-1:123456789012:userpool/us-east-1_AbCdEf",
            ClockSkewMinutes = 2,
        });

        Assert.Equal(ValidateOptionsResult.Success, result);
    }

    private static ChassisJwtOptionsValidator CreateValidator(string environmentName)
    {
        return new ChassisJwtOptionsValidator(new HostEnvironmentStub { EnvironmentName = environmentName });
    }
}
