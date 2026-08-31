using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Application.Features.Organizations.LeaveOrganization;

public sealed record LeaveOrganizationCommand(Guid OrganizationId) : ICommand;

internal sealed class LeaveOrganizationCommandHandler : ICommandHandler<LeaveOrganizationCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public LeaveOrganizationCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(
        LeaveOrganizationCommand request,
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

        if (membership is null)
        {
            return Result.Failure(OrganizationErrors.NotFound);
        }

        try
        {
            membership.Leave(_timeProvider.GetUtcNow());
        }
        catch (DomainException)
        {
            return Result.Failure(
                membership.Role is Domain.Common.Enums.OrganizationRole.Founder
                    ? OrganizationErrors.FounderCannotLeave
                    : OrganizationErrors.NotApprovedMember);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
