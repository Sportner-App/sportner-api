using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Sportner.Application.Behaviors;

public sealed class RequestLoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const long SlowRequestThresholdMilliseconds = 500;

    private readonly ILogger<RequestLoggingBehavior<TRequest, TResponse>> _logger;

    public RequestLoggingBehavior(
        ILogger<RequestLoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Handling application request {RequestName}", requestName);

        try
        {
            return await next();
        }
        finally
        {
            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds >= SlowRequestThresholdMilliseconds)
            {
                _logger.LogWarning(
                    "Slow application request {RequestName} completed in {ElapsedMilliseconds} ms",
                    requestName,
                    stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogInformation(
                    "Application request {RequestName} completed in {ElapsedMilliseconds} ms",
                    requestName,
                    stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
