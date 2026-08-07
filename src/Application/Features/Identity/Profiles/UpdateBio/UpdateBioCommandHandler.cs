using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.Profiles.UpdateBio;

internal sealed class UpdateBioCommandHandler
    : ProfileUpdateHandlerBase, ICommandHandler<UpdateBioCommand, MyProfileResponse>
{
    public UpdateBioCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
        : base(dbContext, currentUser, timeProvider)
    {
    }

    public Task<Result<MyProfileResponse>> Handle(
        UpdateBioCommand request,
        CancellationToken cancellationToken) =>
        UpdateAsync(
            (profile, utcNow) =>
            {
                profile.UpdateBio(request.Bio, utcNow);
                return Result.Success();
            },
            cancellationToken);
}
