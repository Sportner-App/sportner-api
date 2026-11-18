using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Organizations.RotateInviteCode;

public sealed record RotateInviteCodeCommand(Guid OrganizationId) : ICommand<OrganizationDetailResponse>;

internal sealed class RotateInviteCodeCommandHandler
    : ICommandHandler<RotateInviteCodeCommand, OrganizationDetailResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public RotateInviteCodeCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<OrganizationDetailResponse>> Handle(
        RotateInviteCodeCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.NotAuthenticated);
        }

        var membership = await OrganizationQueries.FindMembershipAsync(
            _dbContext,
            request.OrganizationId,
            userId,
            cancellationToken);

        if (membership is null || !membership.CanManageMembers)
        {
            return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.CannotManageMembers);
        }

        var organization = await _dbContext.Organizations
            .FirstOrDefaultAsync(candidate => candidate.Id == request.OrganizationId, cancellationToken);

        if (organization is null)
        {
            return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.NotFound);
        }

        string inviteCode;
        try
        {
            inviteCode = await OrganizationQueries.AllocateInviteCodeAsync(_dbContext, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return Result<OrganizationDetailResponse>.Failure(OrganizationErrors.InviteCodeUnavailable);
        }

        organization.RotateInviteCode(inviteCode, _timeProvider.GetUtcNow());
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = await OrganizationQueries.GetDetailAsync(
            _dbContext,
            organization.Id,
            userId,
            cancellationToken);

        return Result<OrganizationDetailResponse>.Success(response!);
    }
}
