using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Users;

namespace Sportner.Application.Features.Identity.UserProfiles.Streaks;

public sealed record UpdateWeeklyGoalCommand(int WeeklyGoal) : ICommand<StreakResponse>;

public sealed class UpdateWeeklyGoalCommandValidator : AbstractValidator<UpdateWeeklyGoalCommand>
{
    public UpdateWeeklyGoalCommandValidator()
    {
        RuleFor(command => command.WeeklyGoal)
            .InclusiveBetween(UserProfile.MinWeeklyGoal, UserProfile.MaxWeeklyGoal);
    }
}

internal sealed class UpdateWeeklyGoalCommandHandler
    : ICommandHandler<UpdateWeeklyGoalCommand, StreakResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public UpdateWeeklyGoalCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<StreakResponse>> Handle(
        UpdateWeeklyGoalCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<StreakResponse>.Failure(ProfileErrors.NotAuthenticated);
        }

        var profile = await _dbContext.UserProfiles
            .FirstOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken);

        if (profile is null)
        {
            return Result<StreakResponse>.Failure(ProfileErrors.NotFound);
        }

        var utcNow = _timeProvider.GetUtcNow();
        profile.UpdateWeeklyGoal(request.WeeklyGoal, utcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<StreakResponse>.Success(await StreakQueries.BuildAsync(
            _dbContext, userId, profile.WeeklyGoal, utcNow, cancellationToken));
    }
}
