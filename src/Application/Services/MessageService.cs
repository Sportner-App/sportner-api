using System.Net;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.DTOs.Messages;
using Sportner.Application.Mappers;
using Sportner.Domain.Abstractions;
using Sportner.Domain.Data.Interfaces;
using Sportner.Domain.Entities;
using Sportner.Domain.Enums;
using Sportner.Domain.Exceptions;
using Sportner.Localization.Resources;

namespace Sportner.Application.Services;

public class MessageService(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IMessageService
{
    public async Task<IReadOnlyList<MessageDto>> GetMessagesAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId();

        var eventEntity = await unitOfWork.Events.FindOneAsync(e => e.Id == eventId, cancellationToken)
            ?? throw new ApiException(HttpStatusCode.NotFound, ValidationResource.Exception_Event_NotFound);

        if (!await CanAccessChatAsync(eventId, eventEntity.CreatedBy, userId, cancellationToken))
        {
            throw new ApiException(HttpStatusCode.Forbidden, ValidationResource.Exception_Message_Forbidden);
        }

        var messages = await unitOfWork.Messages
            .AsQueryable()
            .AsNoTracking()
            .Include(m => m.User)
            .Where(m => m.EventId == eventId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        return messages.Select(m => m.ToDto()).ToList();
    }

    public async Task<MessageDto> CreateMessageAsync(
        Guid eventId,
        CreateMessageDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId();

        var eventEntity = await unitOfWork.Events.FindOneAsync(e => e.Id == eventId, cancellationToken)
            ?? throw new ApiException(HttpStatusCode.NotFound, ValidationResource.Exception_Event_NotFound);

        if (!await CanAccessChatAsync(eventId, eventEntity.CreatedBy, userId, cancellationToken))
        {
            throw new ApiException(HttpStatusCode.Forbidden, ValidationResource.Exception_Message_Forbidden);
        }

        var content = dto.Content.Trim();
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ApiException(HttpStatusCode.BadRequest, ValidationResource.Exception_Message_Empty);
        }

        var message = new Message
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };

        await unitOfWork.Messages.InsertOneAsync(message, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        message.User = await unitOfWork.Profiles.FindByIdAsync(userId, cancellationToken);

        return message.ToDto();
    }

    private async Task<bool> CanAccessChatAsync(
        Guid eventId,
        Guid createdBy,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (createdBy == userId)
        {
            return true;
        }

        var approvedStatus = ParticipantStatus.Approved.ToDbValue();
        return await unitOfWork.EventParticipants.AnyAsync(
            p => p.EventId == eventId &&
                 p.UserId == userId &&
                 p.Status == approvedStatus,
            cancellationToken);
    }

    private Guid RequireUserId() =>
        currentUser.UserId
        ?? throw new ApiException(HttpStatusCode.Unauthorized, ValidationResource.Exception_Unauthorized);
}
