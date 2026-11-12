namespace QLS.Chassis.WebApi.Auth;

/// <summary>
/// Thrown by <see cref="IUserContextService.GetRequiredUserId"/> when there is no authenticated user or the user ID
/// is not a GUID.
/// </summary>
/// <remarks>
/// <see cref="Exceptions.GlobalExceptionHandler"/> returns it as a 401 Unauthorized problem details response.
/// </remarks>
public sealed class UserContextUnavailableException : Exception
{
    /// <summary>
    /// Creates the exception.
    /// </summary>
    /// <param name="message">The reason the user is unavailable.</param>
    public UserContextUnavailableException(string message) : base(message)
    {
    }
}
