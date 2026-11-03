using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.UserProfiles.UpdateLeaderboardVisibility;

public sealed record UpdateLeaderboardVisibilityCommand(bool ShowOnLeaderboards) : ICommand<MyProfileResponse>;

internal sealed class UpdateLeaderboardVisibilityCommandHandler
    : ProfileUpdateHandlerBase, ICommandHandler<UpdateLeaderboardVisibilityCommand, MyProfileResponse>
{
    public UpdateLeaderboardVisibilityCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
        : base(dbContext, currentUser, timeProvider)
    {
    }

    public Task<Result<MyProfileResponse>> Handle(
        UpdateLeaderboardVisibilityCommand request,
        CancellationToken cancellationToken) =>
        UpdateAsync(
            (profile, utcNow) =>
            {
                profile.UpdateLeaderboardVisibility(request.ShowOnLeaderboards, utcNow);
                return Result.Success();
            },
            cancellationToken);
}
