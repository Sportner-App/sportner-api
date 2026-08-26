using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Social;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;
using Sportner.Domain.Events;

namespace Sportner.Application.Features.Events.AssignEventParticipants;

public sealed record GuestAssignmentRequest(string? FirstName, string? LastName);

public sealed record AssignEventParticipantsCommand(
    Guid EventId,
    IReadOnlyList<GuestAssignmentRequest>? Guests,
    IReadOnlyList<Guid>? FriendUserIds) : ICommand<EventResponse>;

public sealed class AssignEventParticipantsCommandValidator
    : AbstractValidator<AssignEventParticipantsCommand>
{
    public AssignEventParticipantsCommandValidator()
    {
        RuleFor(command => command.EventId).NotEmpty();

        RuleForEach(command => command.Guests)
            .ChildRules(guest =>
            {
                guest.RuleFor(item => item.FirstName)
                    .MaximumLength(EventParticipant.GuestNameMaxLength)
                    .When(item => item.FirstName is not null);

                guest.RuleFor(item => item.LastName)
                    .MaximumLength(EventParticipant.GuestNameMaxLength)
                    .When(item => item.LastName is not null);
            })
            .When(command => command.Guests is not null);

        RuleForEach(command => command.FriendUserIds)
            .NotEmpty()
            .When(command => command.FriendUserIds is not null);
    }
}

internal sealed class AssignEventParticipantsCommandHandler
    : OrganizerEventMutationHandlerBase, ICommandHandler<AssignEventParticipantsCommand, EventResponse>
{
    private readonly INotificationPublisher _notificationPublisher;

    public AssignEventParticipantsCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        INotificationPublisher notificationPublisher)
        : base(dbContext, currentUser, timeProvider)
    {
        _notificationPublisher = notificationPublisher;
    }

    public Task<Result<EventResponse>> Handle(
        AssignEventParticipantsCommand request,
        CancellationToken cancellationToken) =>
        MutateAsync(
            request.EventId,
            async (@event, utcNow, ct) =>
            {
                var guests = request.Guests ?? [];
                var friendIds = (request.FriendUserIds ?? [])
                    .Where(userId => userId != Guid.Empty)
                    .Distinct()
                    .ToList();

                if (guests.Count == 0 && friendIds.Count == 0)
                {
                    return Result.Failure(EventErrors.AssignmentEmpty);
                }

                if (friendIds.Contains(@event.OrganizerUserId))
                {
                    return Result.Failure(EventErrors.FriendAlreadyAssociated);
                }

                if (friendIds.Count > 0)
                {
                    var acceptedFriendIds = await SocialQueries.AcceptedFriendIds(DbContext, @event.OrganizerUserId)
                        .ToListAsync(ct);

                    if (friendIds.Except(acceptedFriendIds).Any())
                    {
                        return Result.Failure(EventErrors.NotFriends);
                    }

                    var existingUserCount = await DbContext.Users
                        .CountAsync(user => friendIds.Contains(user.Id), ct);

                    if (existingUserCount != friendIds.Count)
                    {
                        return Result.Failure(EventErrors.UserNotFound);
                    }

                    var alreadyAssociated = @event.Participants.Any(participant =>
                        participant.UserId is { } userId
                        && friendIds.Contains(userId)
                        && participant.Status is not ParticipantStatus.Cancelled
                        && participant.Status is not ParticipantStatus.Pending);

                    if (alreadyAssociated)
                    {
                        return Result.Failure(EventErrors.FriendAlreadyAssociated);
                    }
                }

                var existingIds = @event.Participants.Select(participant => participant.Id).ToHashSet();
                IReadOnlyList<EventParticipant> assigned;

                try
                {
                    assigned = @event.AssignParticipants(
                        guests.Select(guest => new GuestAssignment(guest.FirstName, guest.LastName)).ToList(),
                        friendIds,
                        utcNow);
                }
                catch (DomainException exception)
                    when (exception.Message == "Event capacity is full.")
                {
                    return Result.Failure(EventErrors.CapacityFull);
                }

                foreach (var participant in assigned)
                {
                    if (!existingIds.Contains(participant.Id))
                    {
                        DbContext.MarkAsAdded(participant);
                    }

                    if (participant.UserId is not { } userId)
                    {
                        continue;
                    }

                    await EventAccess.AddConversationMemberIfPresentAsync(
                        DbContext,
                        @event.Id,
                        userId,
                        utcNow,
                        ct);

                    var statistics = await DbContext.UserStatistics
                        .FirstOrDefaultAsync(candidate => candidate.UserId == userId, ct);

                    statistics?.IncreaseEventsJoined(utcNow);

                    await _notificationPublisher.PublishAsync(
                        userId,
                        NotificationType.EventInvitation,
                        "Etkinliğe eklendin",
                        $"\"{@event.Title}\" etkinliğine eklendin.",
                        NotificationEntityType.Event,
                        @event.Id,
                        @event.OrganizerUserId,
                        ct);
                }

                return Result.Success();
            },
            cancellationToken);
}
