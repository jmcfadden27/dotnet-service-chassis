namespace QLS.Chassis.WebApi.Auth;

/// <summary>
/// Reads the current request's user from the token's claims. For each value the first claim found wins.
/// </summary>
/// <remarks>
/// Registered as a scoped service by <see cref="Extensions.ServiceCollectionExtensions.AddChassisWebApi"/>. Use
/// <see cref="IUserContextService"/> instead when user IDs are GUIDs.
/// </remarks>
public interface ICurrentUserAccessor
{
    /// <summary>
    /// Whether the current request has an authenticated user.
    /// </summary>
    bool IsAuthenticated { get; }
    /// <summary>
    /// The user's identifier, from the <c>sub</c>, name identifier, <c>cognito:username</c> or <c>username</c> claim,
    /// or <see langword="null"/> when none is present.
    /// </summary>
    string? Subject { get; }
    /// <summary>
    /// The user's email, from the <c>email</c> or <see cref="System.Security.Claims.ClaimTypes.Email"/> claim, or
    /// <see langword="null"/>.
    /// </summary>
    string? Email { get; }
    /// <summary>
    /// The user's first name, from the <c>given_name</c> or <see cref="System.Security.Claims.ClaimTypes.GivenName"/>
    /// claim, or <see langword="null"/>.
    /// </summary>
    string? FirstName { get; }
    /// <summary>
    /// The user's last name, from the <c>family_name</c> or <see cref="System.Security.Claims.ClaimTypes.Surname"/>
    /// claim, or <see langword="null"/>.
    /// </summary>
    string? LastName { get; }
    /// <summary>
    /// The user's phone number, from the <c>phone_number</c>, mobile phone or home phone claim, or
    /// <see langword="null"/>.
    /// </summary>
    string? Phone { get; }
}
