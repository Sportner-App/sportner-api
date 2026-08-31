using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Notifications;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Application.Features.Organizations.RejectOrganizationMember;

public sealed record RejectOrganizationMemberCommand(Guid OrganizationId, Guid UserId) : ICommand;

internal sealed class RejectOrganizationMemberCommandHandler
    : ICommandHandler<RejectOrganizationMemberCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly INotificationPublisher _notificationPublisher;

    public RejectOrganizationMemberCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        INotificationPublisher notificationPublisher)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _notificationPublisher = notificationPublisher;
    }

    public async Task<Result> Handle(
        RejectOrganizationMemberCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } actorId)
        {
            return Result.Failure(OrganizationErrors.NotAuthenticated);
        }

        var actor = await OrganizationQueries.FindMembershipAsync(
            _dbContext,
            request.OrganizationId,
            actorId,
            cancellationToken);

        if (actor is null || !actor.CanManageMembers)
        {
            return Result.Failure(OrganizationErrors.CannotManageMembers);
        }

        var membership = await OrganizationQueries.FindMembershipAsync(
            _dbContext,
            request.OrganizationId,
            request.UserId,
            cancellationToken);

        if (membership is null)
        {
            return Result.Failure(OrganizationErrors.MemberNotFound);
        }

        try
        {
            membership.Reject(_timeProvider.GetUtcNow());
        }
        catch (DomainException)
        {
            return Result.Failure(OrganizationErrors.MemberNotFound);
        }

        var title = await NotificationActor.TitleAsync(
            _dbContext,
            actorId,
            "organizasyon katılımını reddetti",
            cancellationToken);

        await _notificationPublisher.PublishAsync(
            request.UserId,
            NotificationType.OrganizationJoinRejected,
            title,
            title,
            NotificationEntityType.Organization,
            request.OrganizationId,
            actorId,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
