using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Notifications;
using Sportner.Domain.Common.Enums;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Application.Features.Organizations.RemoveOrganizationMember;

public sealed record RemoveOrganizationMemberCommand(Guid OrganizationId, Guid UserId) : ICommand;

internal sealed class RemoveOrganizationMemberCommandHandler
    : ICommandHandler<RemoveOrganizationMemberCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly INotificationPublisher _notificationPublisher;

    public RemoveOrganizationMemberCommandHandler(
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
        RemoveOrganizationMemberCommand request,
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

        var target = await OrganizationQueries.FindMembershipAsync(
            _dbContext,
            request.OrganizationId,
            request.UserId,
            cancellationToken);

        if (actor is null || target is null)
        {
            return Result.Failure(OrganizationErrors.MemberNotFound);
        }

        if (!actor.CanModerate(target))
        {
            return Result.Failure(OrganizationErrors.CannotModerateMember);
        }

        try
        {
            target.Remove(_timeProvider.GetUtcNow());
        }
        catch (DomainException)
        {
            return Result.Failure(OrganizationErrors.MemberNotFound);
        }

        var title = await NotificationActor.TitleAsync(
            _dbContext,
            actorId,
            "seni organizasyondan çıkardı",
            cancellationToken);

        await _notificationPublisher.PublishAsync(
            request.UserId,
            NotificationType.OrganizationMemberRemoved,
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
