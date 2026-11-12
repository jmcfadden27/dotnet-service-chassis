namespace QLS.Chassis.WebApi.Auth;

/// <summary>
/// Reads the current request's user from the token's claims, treating the user ID as a <see cref="Guid"/>.
/// </summary>
/// <remarks>
/// Registered as a scoped service by <see cref="Extensions.ServiceCollectionExtensions.AddChassisWebApi"/>. The user
/// ID is read from the <c>sub</c>, name identifier, <c>username</c> or <c>cognito:username</c> claim, first found
/// wins. Use <see cref="ICurrentUserAccessor"/> when the subject is not a GUID.
/// </remarks>
public interface IUserContextService
{
    /// <summary>
    /// Gets the authenticated user's ID.
    /// </summary>
    /// <returns>
    /// The user ID, or <see langword="null"/> when there is no authenticated user, no ID claim, or the ID is not a GUID.
    /// </returns>
    Guid? GetUserId();
    /// <summary>
    /// Gets the authenticated user's ID, failing when it is not available.
    /// </summary>
    /// <returns>The user ID.</returns>
    /// <exception cref="UserContextUnavailableException">
    /// There is no authenticated user or ID claim, or the ID is not a GUID.
    /// </exception>
    Guid GetRequiredUserId();
    /// <summary>
    /// Gets the user's first name from the <c>given_name</c> or
    /// <see cref="System.Security.Claims.ClaimTypes.GivenName"/> claim.
    /// </summary>
    /// <returns>The first name, or <see langword="null"/>.</returns>
    string? GetFirstName();
    /// <summary>
    /// Gets the user's last name from the <c>family_name</c> or <see cref="System.Security.Claims.ClaimTypes.Surname"/>
    /// claim.
    /// </summary>
    /// <returns>The last name, or <see langword="null"/>.</returns>
    string? GetLastName();
    /// <summary>
    /// Gets the user's email from the <c>email</c> or <see cref="System.Security.Claims.ClaimTypes.Email"/> claim.
    /// </summary>
    /// <returns>The email, or <see langword="null"/>.</returns>
    string? GetEmail();
    /// <summary>
    /// Gets the user's phone number from the <c>phone_number</c>, mobile phone or home phone claim.
    /// </summary>
    /// <returns>The phone number, or <see langword="null"/>.</returns>
    string? GetPhone();
}
