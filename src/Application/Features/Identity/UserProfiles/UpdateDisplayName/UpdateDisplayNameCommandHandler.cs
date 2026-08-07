using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.UserProfiles.UpdateDisplayName;

internal sealed class UpdateDisplayNameCommandHandler
    : ProfileUpdateHandlerBase, ICommandHandler<UpdateDisplayNameCommand, MyProfileResponse>
{
    public UpdateDisplayNameCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
        : base(dbContext, currentUser, timeProvider)
    {
    }

    public Task<Result<MyProfileResponse>> Handle(
        UpdateDisplayNameCommand request,
        CancellationToken cancellationToken) =>
        UpdateAsync(
            (profile, utcNow) =>
            {
                profile.UpdateDisplayName(request.FirstName, request.LastName, utcNow);
                return Result.Success();
            },
            cancellationToken);
}
