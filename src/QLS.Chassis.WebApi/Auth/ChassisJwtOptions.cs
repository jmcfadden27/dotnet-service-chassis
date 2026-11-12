namespace QLS.Chassis.WebApi.Auth;

/// <summary>
/// JWT bearer settings, bound from the <c>Auth</c> configuration section by
/// <see cref="Extensions.ServiceCollectionExtensions.AddChassisJwtAuthentication"/> and validated at start-up by
/// <see cref="ChassisJwtOptionsValidator"/>.
/// </summary>
public sealed class ChassisJwtOptions
{
    /// <summary>
    /// The token issuer's URL, or an AWS Cognito user pool ARN
    /// (<c>arn:aws:cognito-idp:&lt;region&gt;:&lt;account&gt;:userpool/&lt;pool-id&gt;</c>), which is turned into the
    /// pool's issuer URL. Required.
    /// </summary>
    public string Authority { get; set; } = string.Empty;
    /// <summary>
    /// The audience (<c>aud</c>) tokens must have. Checked only when set; required in every environment except
    /// Development.
    /// </summary>
    public string Audience { get; set; } = string.Empty;
    /// <summary>
    /// Whether the issuer's metadata must be fetched over HTTPS. Defaults to <see langword="true"/>; set it to
    /// <see langword="false"/> only for a local issuer over plain HTTP.
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;
    /// <summary>
    /// The clock skew allowed when checking token lifetime, in minutes. Must be between 0 and 10. Defaults to 2.
    /// </summary>
    public int ClockSkewMinutes { get; set; } = 2;
}
