using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace QLS.Chassis.WebApi.Auth;

/// <summary>
/// <see cref="IUserContextService"/> that reads claims from <see cref="HttpContext.User"/>.
/// </summary>
public sealed class UserContextService : IUserContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Creates the service.
    /// </summary>
    /// <param name="httpContextAccessor">Gives access to the current request.</param>
    public UserContextService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public Guid? GetUserId()
    {
        var raw = GetUserIdRaw();
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        return Guid.TryParse(raw, out var userId) ? userId : null;
    }

    /// <inheritdoc />
    public Guid GetRequiredUserId()
    {
        var raw = GetUserIdRaw();
        if (string.IsNullOrWhiteSpace(raw))
            throw new UserContextUnavailableException("Authenticated user is required.");

        if (!Guid.TryParse(raw, out var userId))
            throw new UserContextUnavailableException("Authenticated user identifier is invalid.");

        return userId;
    }

    /// <inheritdoc />
    public string? GetFirstName()
    {
        return User?.FindFirstValue("given_name")
            ?? User?.FindFirstValue(ClaimTypes.GivenName);
    }

    /// <inheritdoc />
    public string? GetLastName()
    {
        return User?.FindFirstValue("family_name")
            ?? User?.FindFirstValue(ClaimTypes.Surname);
    }

    /// <inheritdoc />
    public string? GetEmail()
    {
        return User?.FindFirstValue("email")
            ?? User?.FindFirstValue(ClaimTypes.Email);
    }

    /// <inheritdoc />
    public string? GetPhone()
    {
        return User?.FindFirstValue("phone_number")
            ?? User?.FindFirstValue(ClaimTypes.MobilePhone)
            ?? User?.FindFirstValue(ClaimTypes.HomePhone);
    }

    private string? GetUserIdRaw()
    {
        if (User?.Identity?.IsAuthenticated != true)
            return null;

        return User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("username")
            ?? User.FindFirstValue("cognito:username");
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;
}
