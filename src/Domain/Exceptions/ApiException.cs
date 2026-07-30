using System.Net;

namespace Sportner.Domain.Exceptions;

public class ApiException : Exception
{
    public HttpStatusCode HttpStatusCode { get; }

    public ApiException(HttpStatusCode httpStatusCode, string message)
        : base(message)
    {
        HttpStatusCode = httpStatusCode;
    }

    public ApiException(HttpStatusCode httpStatusCode, string message, Exception innerException)
        : base(message, innerException)
    {
        HttpStatusCode = httpStatusCode;
    }
}
