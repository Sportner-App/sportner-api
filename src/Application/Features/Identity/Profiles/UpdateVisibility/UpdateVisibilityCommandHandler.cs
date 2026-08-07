using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.Profiles.UpdateVisibility;

internal sealed class UpdateVisibilityCommandHandler
    : ProfileUpdateHandlerBase, ICommandHandler<UpdateVisibilityCommand, MyProfileResponse>
{
    public UpdateVisibilityCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
        : base(dbContext, currentUser, timeProvider)
    {
    }

    public Task<Result<MyProfileResponse>> Handle(
        UpdateVisibilityCommand request,
        CancellationToken cancellationToken) =>
        UpdateAsync(
            (profile, utcNow) =>
            {
                profile.UpdateVisibility(request.IsProfilePublic, utcNow);
                return Result.Success();
            },
            cancellationToken);
}
