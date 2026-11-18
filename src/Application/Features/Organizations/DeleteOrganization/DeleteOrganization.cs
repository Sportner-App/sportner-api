using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Events;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Organizations.DeleteOrganization;

public sealed record DeleteOrganizationCommand(Guid OrganizationId) : ICommand;

internal sealed class DeleteOrganizationCommandHandler : ICommandHandler<DeleteOrganizationCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly INotificationPublisher _notificationPublisher;

    public DeleteOrganizationCommandHandler(
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
        DeleteOrganizationCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure(OrganizationErrors.NotAuthenticated);
        }

        var membership = await OrganizationQueries.FindMembershipAsync(
            _dbContext,
            request.OrganizationId,
            userId,
            cancellationToken);

        if (membership is null || !membership.IsApproved || membership.Role is not OrganizationRole.Founder)
        {
            return Result.Failure(OrganizationErrors.NotFounder);
        }

        var organization = await _dbContext.Organizations
            .FirstOrDefaultAsync(candidate => candidate.Id == request.OrganizationId, cancellationToken);

        if (organization is null)
        {
            return Result.Failure(OrganizationErrors.NotFound);
        }

        var utcNow = _timeProvider.GetUtcNow();
        organization.MarkDeleted(utcNow);

        var upcomingEvents = await _dbContext.Events
            .Include(candidate => candidate.Participants)
            .Include(candidate => candidate.Waitlist)
            .Where(candidate =>
                candidate.OrganizationId == request.OrganizationId
                // Başlamış etkinliği iptal etme: tamamlama worker'ı henüz Completed yapmamış olabilir
                // (EventCompletionDispatcher da EventDate <= now ile başlar).
                && candidate.EventDate > utcNow
                && (candidate.Status == EventStatus.Draft
                    || candidate.Status == EventStatus.Published
                    || candidate.Status == EventStatus.Full))
            .ToListAsync(cancellationToken);

        foreach (var @event in upcomingEvents)
        {
            await EventCancellation.CancelAsync(
                _dbContext, _notificationPublisher, @event, userId, utcNow, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
