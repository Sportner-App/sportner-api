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

    /// <summary>
    /// Tells the given users' connections (group <c>user:{id}</c>) that a conversation changed,
    /// so conversation lists can refetch. Payload carries only the conversation id.
    /// </summary>
    Task NotifyConversationUpdatedAsync(
        Guid conversationId,
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default);
}
