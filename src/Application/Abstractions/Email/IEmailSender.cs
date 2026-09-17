namespace Sportner.Application.Abstractions.Email;

public sealed record EmailSendResult(bool Succeeded, string? ErrorMessage)
{
    public static EmailSendResult Ok() => new(true, null);

    public static EmailSendResult Failed(string error) => new(false, error);
}

public interface IEmailSender
{
    Task<EmailSendResult> SendVerificationCodeAsync(
        string toEmail,
        string code,
        CancellationToken cancellationToken = default);

    Task<EmailSendResult> SendPasswordResetCodeAsync(
        string toEmail,
        string code,
        CancellationToken cancellationToken = default);
}
