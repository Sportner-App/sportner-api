namespace Sportner.Application.Abstractions.Email;

public sealed record EmailSendResult(bool Succeeded, string? ErrorMessage)
{
    public static EmailSendResult Ok() => new(true, null);

    public static EmailSendResult Failed(string error) => new(false, error);
}

/// <summary>
/// Kept single-purpose (verification codes only) rather than a generic "send any email" API —
/// that's the only thing the product needs sent right now, and a broader abstraction can be
/// designed once a second use case actually exists.
/// </summary>
public interface IEmailSender
{
    Task<EmailSendResult> SendVerificationCodeAsync(
        string toEmail,
        string code,
        CancellationToken cancellationToken = default);
}
