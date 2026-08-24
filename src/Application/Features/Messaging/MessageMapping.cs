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
        var profile = await dbContext.UserProfiles.AsNoTracking()
            .Where(candidate => candidate.UserId == message.SenderUserId)
            .Select(candidate => new
            {
                candidate.Username,
                candidate.FirstName,
                candidate.LastName,
                candidate.ProfileImageUrl
            })
            .FirstOrDefaultAsync(cancellationToken);

        return ToResponse(
            message,
            profile?.Username,
            profile?.FirstName,
            profile?.LastName,
            profile?.ProfileImageUrl);
    }

    internal static MessageResponse ToResponse(
        Message message,
        string? senderUsername,
        string? senderFirstName,
        string? senderLastName,
        string? senderProfileImageUrl) =>
        new(
            message.Id,
            message.ConversationId,
            message.SenderUserId,
            senderUsername,
            senderFirstName,
            senderLastName,
            senderProfileImageUrl,
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
