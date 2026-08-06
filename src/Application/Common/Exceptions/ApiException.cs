using System.Net;

namespace Sportner.Application.Common.Exceptions;

public sealed class ApiException : Exception
{
    public ApiException(HttpStatusCode statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get; }
}
