using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Notifications;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Organizations.JoinPublicOrganization;

public sealed record JoinPublicOrganizationCommand(Guid OrganizationId) : ICommand<OrganizationDetailResponse>;

internal sealed class JoinPublicOrganizationCommandHandler
    : ICommandHandler<JoinPublicOrganizationCommand, OrganizationDetailResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly INotificationPublisher _notificationPublisher;

    public JoinPublicOrganizationCommandHandler(
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

    public async Task<Result<OrganizationDetailResponse>> Handle(
        JoinPublicOrganizationCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.NotAuthenticated);
        }

        if (await OrganizationJoin.ValidateUserAsync(_dbContext, userId, cancellationToken) is { } userError)
        {
            return Result<OrganizationDetailResponse>.Failure(userError);
        }

        var organization = await _dbContext.Organizations
            .FirstOrDefaultAsync(candidate => candidate.Id == request.OrganizationId, cancellationToken);

        if (organization is null)
        {
            return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.NotFound);
        }

        if (!organization.IsPublic)
        {
            return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.NotPublic);
        }

        return await OrganizationJoin.JoinAsync(
            _dbContext,
            _notificationPublisher,
            _timeProvider,
            organization,
            userId,
            cancellationToken);
    }
}
