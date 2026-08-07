using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.UserProfiles.UpdatePersonalDetails;

internal sealed class UpdatePersonalDetailsCommandHandler
    : ProfileUpdateHandlerBase, ICommandHandler<UpdatePersonalDetailsCommand, MyProfileResponse>
{
    public UpdatePersonalDetailsCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
        : base(dbContext, currentUser, timeProvider)
    {
    }

    public Task<Result<MyProfileResponse>> Handle(
        UpdatePersonalDetailsCommand request,
        CancellationToken cancellationToken) =>
        UpdateAsync(
            (profile, utcNow) =>
            {
                profile.UpdatePersonalDetails(request.Gender, request.BirthDate, utcNow);
                return Result.Success();
            },
            cancellationToken);
}
