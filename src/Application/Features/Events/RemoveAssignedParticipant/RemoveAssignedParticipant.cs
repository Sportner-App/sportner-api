using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Events;

namespace Sportner.Application.Features.Events.RemoveAssignedParticipant;

public sealed record RemoveAssignedParticipantCommand(
    Guid EventId,
    Guid ParticipantId,
    Guid ReportReasonId,
    string? Note)
    : ICommand<EventResponse>;

public sealed class RemoveAssignedParticipantCommandValidator
    : FluentValidation.AbstractValidator<RemoveAssignedParticipantCommand>
{
    public RemoveAssignedParticipantCommandValidator()
    {
        RuleFor(command => command.EventId).NotEmpty();
        RuleFor(command => command.ParticipantId).NotEmpty();
        RuleFor(command => command.ReportReasonId).NotEmpty();
        RuleFor(command => command.Note)
            .MaximumLength(EventParticipantRemoval.NoteMaxLength);
    }
}

internal sealed class RemoveAssignedParticipantCommandHandler
    : OrganizerEventMutationHandlerBase, ICommandHandler<RemoveAssignedParticipantCommand, EventResponse>
{
    public RemoveAssignedParticipantCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
        : base(dbContext, currentUser, timeProvider)
    {
    }

    public Task<Result<EventResponse>> Handle(
        RemoveAssignedParticipantCommand request,
        CancellationToken cancellationToken) =>
        MutateAsync(
            request.EventId,
            async (@event, utcNow, ct) =>
            {
                var participant = @event.Participants
                    .FirstOrDefault(item => item.Id == request.ParticipantId);

                if (participant is null)
                {
                    return Result.Failure(EventErrors.ParticipantNotFound);
                }

                if (participant.UserId == @event.OrganizerUserId)
                {
                    return Result.Failure(EventErrors.NotOrganizer);
                }

                var reasonExists = await DbContext.ReportReasons.AsNoTracking()
                    .AnyAsync(reason =>
                        reason.Id == request.ReportReasonId && reason.IsActive,
                        ct);

                if (!reasonExists)
                {
                    return Result.Failure(EventErrors.RemovalReasonNotFound);
                }

                var userId = participant.UserId;
                var wasApproved = participant.Status is ParticipantStatus.Approved;

                @event.RemoveAssignedParticipant(request.ParticipantId, utcNow);

                DbContext.EventParticipantRemovals.Add(EventParticipantRemoval.Create(
                    @event.Id,
                    participant.Id,
                    @event.OrganizerUserId,
                    userId,
                    request.ReportReasonId,
                    request.Note,
                    utcNow));

                if (userId is { } registeredUserId)
                {
                    await EventAccess.RemoveConversationMemberIfPresentAsync(
                        DbContext,
                        @event.Id,
                        registeredUserId,
                        utcNow,
                        ct);

                    if (wasApproved)
                    {
                        var statistics = await DbContext.UserStatistics
                            .FirstOrDefaultAsync(candidate => candidate.UserId == registeredUserId, ct);

                        if (statistics is not null && statistics.EventsJoined > 0)
                        {
                            statistics.DecreaseEventsJoined(utcNow);
                        }
                    }
                }

                return Result.Success();
            },
            cancellationToken);
}
