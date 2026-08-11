using Sportner.Application.Features.Messaging;

namespace Sportner.Application.Abstractions.Realtime;

/// <summary>
/// Used by hosts that do not run SignalR (workers/tests). Always succeeds as a no-op.
/// </summary>
public sealed class NullChatRealtimeNotifier : IChatRealtimeNotifier
{
    public static NullChatRealtimeNotifier Instance { get; } = new();

    public Task NotifyMessageCreatedAsync(
        Guid conversationId,
        MessageResponse message,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifyMessageEditedAsync(
        Guid conversationId,
        MessageResponse message,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task NotifyMessageRedactedAsync(
        Guid conversationId,
        MessageResponse message,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
