using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.Onboarding.CompleteOnboarding;

internal sealed class CompleteOnboardingCommandHandler : ICommandHandler<CompleteOnboardingCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public CompleteOnboardingCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(
        CompleteOnboardingCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure(OnboardingErrors.NotAuthenticated);
        }

        var user = await _dbContext.Users
            .Include(candidate => candidate.UserProfile)
            .Include(candidate => candidate.Sports)
            .FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(OnboardingErrors.UserNotFound);
        }

        if (user.HasCompletedOnboarding())
        {
            return Result.Success();
        }

        if (user.UserProfile is null)
        {
            return Result.Failure(OnboardingErrors.ProfileRequired);
        }

        if (user.Sports.Count == 0)
        {
            return Result.Failure(OnboardingErrors.SportRequired);
        }

        user.CompleteOnboarding(_timeProvider.GetUtcNow());

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
