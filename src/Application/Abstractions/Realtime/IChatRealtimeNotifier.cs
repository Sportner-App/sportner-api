using Sportner.Application.Features.Messaging;

namespace Sportner.Application.Abstractions.Realtime;

/// <summary>
/// Pushes conversation message events to connected realtime clients.
/// Implementations must be best-effort: failures are logged, never fail the REST write.
/// </summary>
public interface IChatRealtimeNotifier
{
    Task NotifyMessageCreatedAsync(
        Guid conversationId,
        MessageResponse message,
        CancellationToken cancellationToken = default);

    Task NotifyMessageEditedAsync(
        Guid conversationId,
        MessageResponse message,
        CancellationToken cancellationToken = default);

    Task NotifyMessageRedactedAsync(
        Guid conversationId,
        MessageResponse message,
        CancellationToken cancellationToken = default);
}
