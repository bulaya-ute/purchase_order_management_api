using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PurchaseOrderManagement.Api.Services;

namespace PurchaseOrderManagement.Api.Infrastructure;

/// <summary>
/// Translates <see cref="ServiceException"/> thrown from services into a consistent
/// <see cref="ProblemDetails"/> response, so controllers can stay thin and simply call services
/// without wrapping every call in try/catch.
/// </summary>
public class ServiceExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not ServiceException ex)
        {
            return;
        }

        var problem = new ProblemDetails
        {
            Status = ex.StatusCode,
            Title = ReasonFor(ex.StatusCode),
            Detail = ex.Message,
        };

        context.Result = new ObjectResult(problem) { StatusCode = ex.StatusCode };
        context.ExceptionHandled = true;
    }

    private static string ReasonFor(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Bad Request",
        StatusCodes.Status403Forbidden => "Forbidden",
        StatusCodes.Status404NotFound => "Not Found",
        StatusCodes.Status409Conflict => "Conflict",
        StatusCodes.Status422UnprocessableEntity => "Unprocessable Entity",
        _ => "Error",
    };
}
