using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sportner.Application.Common.Results;

namespace Sportner.API.Common;

/// <summary>
/// Maps the Application <see cref="Result"/> / <see cref="Result{T}"/> types to HTTP responses.
/// Failures become RFC 7807 <see cref="ProblemDetails"/> aligned with the global exception handler.
/// </summary>
public static class ApiResults
{
    public static IActionResult ToActionResult(this Result result, int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
        {
            return new StatusCodeResult(successStatusCode);
        }

        return Problem(result.Errors);
    }

    public static IActionResult ToActionResult<T>(
        this Result<T> result,
        int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
        {
            return new ObjectResult(result.Value) { StatusCode = successStatusCode };
        }

        return Problem(result.Errors);
    }

    public static IActionResult ToCreatedResult<T>(
        this Result<T> result,
        string routeName,
        object routeValues)
    {
        if (result.IsSuccess)
        {
            return new CreatedAtRouteResult(routeName, routeValues, result.Value);
        }

        return Problem(result.Errors);
    }

    private static IActionResult Problem(IReadOnlyList<Error> errors)
    {
        var primary = errors[0];
        var status = StatusForType(primary.Type);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = TitleForType(primary.Type),
            Detail = primary.Message
        };

        problem.Extensions["errors"] = errors
            .Select(error => new { error.Code, error.Message, Type = error.Type.ToString() })
            .ToArray();

        return new ObjectResult(problem)
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" }
        };
    }

    private static int StatusForType(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };

    private static string TitleForType(ErrorType type) => type switch
    {
        ErrorType.Validation => "Validation failed",
        ErrorType.Unauthorized => "Unauthorized",
        ErrorType.Forbidden => "Forbidden",
        ErrorType.NotFound => "Resource not found",
        ErrorType.Conflict => "Conflict",
        _ => "An unexpected error occurred"
    };
}
