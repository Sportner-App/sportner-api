using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Sportner.Domain.Exceptions;
using Sportner.Localization.Resources;

namespace Sportner.API.Middleware;

public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, message) = exception switch
        {
            ApiException apiException => (
                (int)apiException.HttpStatusCode,
                apiException.Message),
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                validationException.Errors.FirstOrDefault()?.ErrorMessage
                    ?? validationException.Message),
            _ => (
                StatusCodes.Status500InternalServerError,
                environment.IsDevelopment()
                    ? exception.GetBaseException().Message
                    : ValidationResource.Exception_InternalServerError)
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception");
        }
        else
        {
            logger.LogWarning(exception, "Handled exception ({StatusCode})", statusCode);
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        await JsonSerializer.SerializeAsync(
            httpContext.Response.Body,
            new { message },
            JsonOptions,
            cancellationToken);

        return true;
    }
}
