using Microsoft.AspNetCore.SignalR;
using Sportner.API.Hubs;
using Sportner.Application.Abstractions.Realtime;
using Sportner.Application.Features.Messaging;

namespace Sportner.API.Realtime;

public sealed class SignalRChatRealtimeNotifier : IChatRealtimeNotifier
{
    private readonly IHubContext<ConversationHub> _hubContext;
    private readonly ILogger<SignalRChatRealtimeNotifier> _logger;

    public SignalRChatRealtimeNotifier(
        IHubContext<ConversationHub> hubContext,
        ILogger<SignalRChatRealtimeNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public Task NotifyMessageCreatedAsync(
        Guid conversationId,
        MessageResponse message,
        CancellationToken cancellationToken = default) =>
        TrySendAsync(conversationId, "MessageCreated", message, cancellationToken);

    public Task NotifyMessageEditedAsync(
        Guid conversationId,
        MessageResponse message,
        CancellationToken cancellationToken = default) =>
        TrySendAsync(conversationId, "MessageEdited", message, cancellationToken);

    public Task NotifyMessageRedactedAsync(
        Guid conversationId,
        MessageResponse message,
        CancellationToken cancellationToken = default) =>
        TrySendAsync(conversationId, "MessageRedacted", message, cancellationToken);

    private async Task TrySendAsync(
        Guid conversationId,
        string eventName,
        MessageResponse message,
        CancellationToken cancellationToken)
    {
        try
        {
            await _hubContext.Clients
                .Group(ConversationHub.GroupName(conversationId))
                .SendAsync(eventName, message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to push {EventName} for conversation {ConversationId}, message {MessageId}.",
                eventName,
                conversationId,
                message.Id);
        }
    }
}
