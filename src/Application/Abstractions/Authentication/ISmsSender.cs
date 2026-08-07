namespace Sportner.Application.Abstractions.Authentication;

/// <summary>
/// Delivers SMS messages. Swapped for a real provider in production; a development
/// implementation may no-op, but must never log the message body (it can contain the OTP).
/// </summary>
public interface ISmsSender
{
    Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
}
