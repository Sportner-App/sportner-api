using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Notifications;
using Sportner.Application.Features.Social;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;
using Sportner.Domain.Events;
using Sportner.Localization.Resources;

namespace Sportner.Application.Features.Events.AssignEventParticipants;

public sealed record GuestAssignmentRequest(string? FirstName, string? LastName, string? Email);

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
                    .Must(value => !string.IsNullOrWhiteSpace(value))
                    .WithMessage("Guest first name is required.")
                    .MaximumLength(EventParticipant.GuestNameMaxLength);

                guest.RuleFor(item => item.LastName)
                    .Must(value => !string.IsNullOrWhiteSpace(value))
                    .WithMessage("Guest last name is required.")
                    .MaximumLength(EventParticipant.GuestNameMaxLength);

                guest.RuleFor(item => item.Email)
                    .NotEmpty()
                    .EmailAddress()
                    .MaximumLength(EventParticipant.GuestEmailMaxLength)
                    .WithMessage("A valid guest email is required.");
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

                var organizerActor = await NotificationActor.ResolveEventActorNameAsync(
                    DbContext, @event, @event.OrganizerUserId, ct);

                if (friendIds.Count > 0)
                {
                    // Engel kontrolü işlemi yapan kişiye göre: kulüp yöneticisi oluşturanın
                    // arkadaşlarını değil kendi engellerini kullanır (oluşturan için aynı şey).
                    var actingUserId = CurrentUser.UserId!.Value;

                    if (@event.OrganizationId is { } organizationId)
                    {
                        // Kulüp etkinliği: davet edilenler kulübün onaylı üyesi olmalı (arkadaşlık gerekmez).
                        // Organizations soft-delete filtresine sahip, OrganizationMembers sahip değil.
                        var approvedMemberIds = await (
                                from member in DbContext.OrganizationMembers
                                join organization in DbContext.Organizations
                                    on member.OrganizationId equals organization.Id
                                where member.OrganizationId == organizationId
                                    && member.Status == OrganizationMemberStatus.Approved
                                    && organization.DeletedAt == null
                                    && friendIds.Contains(member.UserId)
                                select member.UserId)
                            .ToListAsync(ct);

                        if (friendIds.Except(approvedMemberIds).Any())
                        {
                            return Result.Failure(EventErrors.InviteeNotClubMember);
                        }
                    }
                    else
                    {
                        var acceptedFriendIds = await SocialQueries.AcceptedFriendIds(DbContext, actingUserId)
                            .ToListAsync(ct);

                        if (friendIds.Except(acceptedFriendIds).Any())
                        {
                            return Result.Failure(EventErrors.NotFriends);
                        }
                    }

                    var blockedFriendIds = await BlockQueries.BlockedUserIds(DbContext, actingUserId)
                        .ToListAsync(ct);

                    if (friendIds.Any(blockedFriendIds.Contains))
                    {
                        return Result.Failure(EventErrors.RelationshipBlocked);
                    }

                    var existingUserCount = await DbContext.Users
                        .CountAsync(
                            user => friendIds.Contains(user.Id)
                                && user.Status != UserStatus.Deleted
                                && user.Status != UserStatus.Banned,
                            ct);

                    if (existingUserCount != friendIds.Count)
                    {
                        return Result.Failure(EventErrors.UserNotFound);
                    }

                    var friendProfiles = await DbContext.UserProfiles.AsNoTracking()
                        .Where(profile => friendIds.Contains(profile.UserId))
                        .Select(profile => new { profile.BirthDate, profile.Gender })
                        .ToListAsync(ct);

                    // Doğum tarihi bilinmiyorsa yaş "aralık dışı" değildir; organizatöre
                    // ne yapacağını söyleyebilmek için iki durumu ayırıyoruz.
                    if (@event.HasAgeRestriction
                        && (friendProfiles.Count != friendIds.Count
                            || friendProfiles.Any(profile => profile.BirthDate is null)))
                    {
                        return Result.Failure(EventErrors.ParticipantBirthDateMissing);
                    }

                    if (friendProfiles.Any(profile =>
                            profile.BirthDate is { } birthDate
                            && !@event.IsParticipantAgeEligible(birthDate)))
                    {
                        return Result.Failure(EventErrors.ParticipantAgeNotEligible);
                    }

                    if (friendProfiles.Any(profile =>
                            @event.IsParticipantGenderMissing(profile.Gender)))
                    {
                        return Result.Failure(EventErrors.ParticipantGenderMissing);
                    }

                    if (friendProfiles.Any(profile =>
                            !@event.IsParticipantGenderEligible(profile.Gender)))
                    {
                        return Result.Failure(EventErrors.ParticipantGenderNotEligible);
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
                        guests.Select(guest => new GuestAssignment(guest.FirstName, guest.LastName, guest.Email)).ToList(),
                        friendIds,
                        utcNow);
                }
                catch (DomainException exception)
                    when (exception.Message == "Event capacity is full.")
                {
                    return Result.Failure(EventErrors.CapacityFull);
                }

                var assignedUserIds = assigned
                    .Where(participant => participant.UserId is not null)
                    .Select(participant => participant.UserId!.Value)
                    .ToList();
                var languagesByRecipient = await NotificationActor.ResolveRecipientLanguagesAsync(
                    DbContext, assignedUserIds, ct);

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

                    if (participant.Status is ParticipantStatus.Approved)
                    {
                        await EventAccess.AddConversationMemberIfPresentAsync(
                            DbContext,
                            @event.Id,
                            userId,
                            utcNow,
                            ct);

                        var statistics = await DbContext.UserStatistics
                            .FirstOrDefaultAsync(candidate => candidate.UserId == userId, ct);

                        statistics?.IncreaseEventsJoined(utcNow);
                    }

                    var language = languagesByRecipient.GetValueOrDefault(userId);

                    await _notificationPublisher.PublishAsync(
                        userId,
                        NotificationType.EventInvitation,
                        NotificationActor.Format(
                            language,
                            nameof(NotificationsResource.EventInvitation_Title),
                            NotificationActor.FormatPrefix(organizerActor, language)),
                        NotificationActor.Format(
                            language,
                            nameof(NotificationsResource.EventInvitation_Body),
                            @event.Title),
                        NotificationEntityType.Event,
                        @event.Id,
                        @event.OrganizerUserId,
                        ct);
                }

                return Result.Success();
            },
            cancellationToken);
}
