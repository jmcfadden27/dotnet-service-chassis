namespace QLS.Chassis.WebApi.Auth;

internal static class CognitoIssuerResolver
{
    public static string Resolve(string authorityOrArn)
    {
        var input = authorityOrArn?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        if (!input.StartsWith("arn:", StringComparison.OrdinalIgnoreCase))
            return input.TrimEnd('/');

        var arnParts = input.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (arnParts.Length < 6)
            return input;

        var region = arnParts[3];
        var resource = arnParts[5];
        var resourceParts = resource.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (resourceParts.Length != 2 || !string.Equals(resourceParts[0], "userpool", StringComparison.OrdinalIgnoreCase))
            return input;

        var userPoolId = resourceParts[1];
        return $"https://cognito-idp.{region}.amazonaws.com/{userPoolId}";
    }
}
