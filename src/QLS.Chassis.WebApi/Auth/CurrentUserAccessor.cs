using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace QLS.Chassis.WebApi.Auth;

/// <summary>
/// <see cref="ICurrentUserAccessor"/> that reads claims from <see cref="HttpContext.User"/>.
/// </summary>
public sealed class CurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Creates the accessor.
    /// </summary>
    /// <param name="httpContextAccessor">Gives access to the current request.</param>
    public CurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    /// <inheritdoc />
    public string? Subject =>
        User?.FindFirstValue("sub")
        ?? User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User?.FindFirstValue("cognito:username")
        ?? User?.FindFirstValue("username");

    /// <inheritdoc />
    public string? Email =>
        User?.FindFirstValue("email")
        ?? User?.FindFirstValue(ClaimTypes.Email);

    /// <inheritdoc />
    public string? FirstName =>
        User?.FindFirstValue("given_name")
        ?? User?.FindFirstValue(ClaimTypes.GivenName);

    /// <inheritdoc />
    public string? LastName =>
        User?.FindFirstValue("family_name")
        ?? User?.FindFirstValue(ClaimTypes.Surname);

    /// <inheritdoc />
    public string? Phone =>
        User?.FindFirstValue("phone_number")
        ?? User?.FindFirstValue(ClaimTypes.MobilePhone)
        ?? User?.FindFirstValue(ClaimTypes.HomePhone);

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;
}
