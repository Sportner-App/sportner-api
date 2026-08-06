using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Sportner.Application.Common.Exceptions;
using Sportner.Domain.Common.Exceptions;
using Sportner.Localization.Resources;

namespace Sportner.API.Middleware;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problem = CreateProblemDetails(httpContext, exception);

        if (problem.Status >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "Unhandled exception for {Method} {Path}. TraceId: {TraceId}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                httpContext.TraceIdentifier);
        }
        else
        {
            _logger.LogWarning(
                exception,
                "Request failed with status {StatusCode}. TraceId: {TraceId}",
                problem.Status,
                httpContext.TraceIdentifier);
        }

        httpContext.Response.StatusCode =
            problem.Status ?? StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private static ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        Exception exception)
    {
        var (status, title, detail) = exception switch
        {
            ValidationException => (
                StatusCodes.Status400BadRequest,
                ValidationResource.Validation_Base_Failed,
                ValidationResource.Validation_Base_Failed),
            DomainException => (
                StatusCodes.Status400BadRequest,
                ValidationResource.Exception_Base_InvalidOperation,
                ValidationResource.Exception_Base_InvalidOperation),
            ApiException apiException => (
                (int)apiException.StatusCode,
                GetTitle((int)apiException.StatusCode),
                apiException.Message),
            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                ValidationResource.Exception_Base_Unauthorized,
                ValidationResource.Exception_Base_Unauthorized),
            _ => (
                StatusCodes.Status500InternalServerError,
                ValidationResource.Exception_Base_Unexpected,
                ValidationResource.Exception_Base_Unexpected)
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        if (exception is ValidationException validationException)
        {
            problem.Extensions["errors"] = validationException.Errors
                .GroupBy(failure => failure.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .Select(failure => failure.ErrorMessage)
                        .Distinct()
                        .ToArray());
        }

        return problem;
    }

    private static string GetTitle(int statusCode) =>
        statusCode switch
        {
            StatusCodes.Status401Unauthorized =>
                ValidationResource.Exception_Base_Unauthorized,
            StatusCodes.Status403Forbidden =>
                ValidationResource.Exception_Base_Forbidden,
            StatusCodes.Status404NotFound =>
                ValidationResource.Exception_Base_NotFound_ByFilter,
            StatusCodes.Status409Conflict =>
                ValidationResource.Exception_Base_Conflict,
            _ => ValidationResource.Exception_Base_InvalidOperation
        };
}
