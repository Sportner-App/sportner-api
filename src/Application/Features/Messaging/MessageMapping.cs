using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Domain.Messaging;

namespace Sportner.Application.Features.Messaging;

internal static class MessageMapping
{
    internal static async Task<MessageResponse> ToResponseAsync(
        IApplicationDbContext dbContext,
        Message message,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.Profiles.AsNoTracking()
            .Where(candidate => candidate.UserId == message.SenderUserId)
            .Select(candidate => new { candidate.Username, candidate.FirstName })
            .FirstOrDefaultAsync(cancellationToken);

        return ToResponse(message, profile?.Username, profile?.FirstName);
    }

    internal static MessageResponse ToResponse(
        Message message,
        string? senderUsername,
        string? senderFirstName) =>
        new(
            message.Id,
            message.ConversationId,
            message.SenderUserId,
            senderUsername,
            senderFirstName,
            (short)message.MessageType,
            message.Content,
            message.MediaUrl,
            message.MediaSize,
            message.MediaMimeType,
            message.ReplyToMessageId,
            message.EditedAt,
            message.IsRedacted(),
            message.CreatedAt);
}
