using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.UserProfiles.UpdateLocation;

internal sealed class UpdateLocationCommandHandler
    : ProfileUpdateHandlerBase, ICommandHandler<UpdateLocationCommand, MyProfileResponse>
{
    public UpdateLocationCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
        : base(dbContext, currentUser, timeProvider)
    {
    }

    public Task<Result<MyProfileResponse>> Handle(
        UpdateLocationCommand request,
        CancellationToken cancellationToken) =>
        UpdateAsync(
            (profile, utcNow) =>
            {
                profile.UpdateLocation(request.City, utcNow);
                return Result.Success();
            },
            cancellationToken);
}
