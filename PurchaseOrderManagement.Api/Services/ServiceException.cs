namespace PurchaseOrderManagement.Api.Services;

/// <summary>
/// Thrown by Admin-slice services to signal an expected, client-facing failure (validation,
/// not-found, forbidden, conflict). Carries the HTTP status the API should return. A controller
/// exception filter (<see cref="Infrastructure.ServiceExceptionFilter"/>) translates it into a
/// standard <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> response, keeping controllers thin.
/// </summary>
public class ServiceException : Exception
{
    public int StatusCode { get; }

    public ServiceException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }

    public static ServiceException NotFound(string message) =>
        new(StatusCodes.Status404NotFound, message);

    public static ServiceException Validation(string message) =>
        new(StatusCodes.Status422UnprocessableEntity, message);

    public static ServiceException Conflict(string message) =>
        new(StatusCodes.Status409Conflict, message);

    public static ServiceException Forbidden(string message) =>
        new(StatusCodes.Status403Forbidden, message);

    public static ServiceException BadRequest(string message) =>
        new(StatusCodes.Status400BadRequest, message);
}
