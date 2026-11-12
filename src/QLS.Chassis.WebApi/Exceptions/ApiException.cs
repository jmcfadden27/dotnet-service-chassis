namespace QLS.Chassis.WebApi.Exceptions;

/// <summary>
/// An error that is part of the API's contract, returned to the client with a chosen status code and title.
/// </summary>
/// <remarks>
/// <see cref="GlobalExceptionHandler"/> returns it as a problem details response with
/// <see cref="StatusCode"/>, <see cref="Title"/> and the exception's message as the detail.
/// </remarks>
public class ApiException : Exception
{
    /// <summary>
    /// The HTTP status code to return.
    /// </summary>
    public int StatusCode { get; }
    /// <summary>
    /// The problem details title.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Creates the exception.
    /// </summary>
    /// <param name="statusCode">The HTTP status code to return.</param>
    /// <param name="title">The problem details title.</param>
    /// <param name="detail">
    /// The problem details detail, used as the exception message. When <see langword="null"/>, <paramref name="title"/>
    /// is used instead. It is shown to the client, so keep it free of internal details.
    /// </param>
    public ApiException(int statusCode, string title, string? detail = null)
        : base(detail ?? title)
    {
        StatusCode = statusCode;
        Title = title;
    }
}
