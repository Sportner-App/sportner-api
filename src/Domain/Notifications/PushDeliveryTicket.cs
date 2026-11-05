using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Notifications;

/// <summary>
/// One accepted push per device. Expo's "ok" at send time only means it queued the message;
/// the receipt fetched later says whether APNs/FCM actually took it.
/// </summary>
public class PushDeliveryTicket : AuditableEntity
{
    private const int MaxErrorLength = 1000;
    private const int MaxErrorCodeLength = 100;

    private PushDeliveryTicket()
    {
    }

    public Guid OutboxId { get; private set; }

    public Guid UserDeviceId { get; private set; }

    public string TicketId { get; private set; } = null!;

    /// <summary>The token used for this send; a device may have re-registered a new one since.</summary>
    public string PushToken { get; private set; } = null!;

    public PushTicketStatus Status { get; private set; }

    public DateTimeOffset CheckAfter { get; private set; }

    public DateTimeOffset? CheckedAt { get; private set; }

    public string? ErrorCode { get; private set; }

    public string? ErrorMessage { get; private set; }

    public static PushDeliveryTicket Create(
        Guid outboxId,
        Guid userDeviceId,
        string ticketId,
        string pushToken,
        DateTimeOffset checkAfter,
        DateTimeOffset utcNow)
    {
        if (string.IsNullOrWhiteSpace(ticketId))
        {
            throw new DomainException("Push ticket id is required.");
        }

        if (string.IsNullOrWhiteSpace(pushToken))
        {
            throw new DomainException("Push token is required.");
        }

        return new PushDeliveryTicket
        {
            Id = Guid.NewGuid(),
            OutboxId = outboxId,
            UserDeviceId = userDeviceId,
            TicketId = ticketId,
            PushToken = pushToken,
            Status = PushTicketStatus.Pending,
            CheckAfter = checkAfter,
            CreatedAt = utcNow
        };
    }

    public void MarkDelivered(DateTimeOffset utcNow) => Resolve(PushTicketStatus.Delivered, null, null, utcNow);

    public void MarkFailed(string? errorCode, string? errorMessage, DateTimeOffset utcNow) =>
        Resolve(PushTicketStatus.Failed, errorCode, errorMessage, utcNow);

    public void MarkExpired(DateTimeOffset utcNow) => Resolve(PushTicketStatus.Expired, null, null, utcNow);

    public void Reschedule(DateTimeOffset checkAfter, DateTimeOffset utcNow)
    {
        CheckAfter = checkAfter;
        UpdatedAt = utcNow;
    }

    private void Resolve(
        PushTicketStatus status,
        string? errorCode,
        string? errorMessage,
        DateTimeOffset utcNow)
    {
        Status = status;
        ErrorCode = Truncate(errorCode, MaxErrorCodeLength);
        ErrorMessage = Truncate(errorMessage, MaxErrorLength);
        CheckedAt = utcNow;
        UpdatedAt = utcNow;
    }

    private static string? Truncate(string? value, int maxLength) =>
        value is null || value.Length <= maxLength ? value : value[..maxLength];
}
