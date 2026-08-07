using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Identity.UserSports.ChangeSportSkillLevel;

internal sealed class ChangeSportSkillLevelCommandHandler
    : UserSportMutationHandlerBase,
        ICommandHandler<ChangeSportSkillLevelCommand, IReadOnlyList<UserSportResponse>>
{
    public ChangeSportSkillLevelCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
        : base(dbContext, currentUser, timeProvider)
    {
    }

    public Task<Result<IReadOnlyList<UserSportResponse>>> Handle(
        ChangeSportSkillLevelCommand request,
        CancellationToken cancellationToken) =>
        MutateAsync(
            request.SportId,
            (user, utcNow) => user.ChangeSportSkillLevel(
                request.SportId,
                (SkillLevel)request.SkillLevel,
                utcNow),
            cancellationToken);
}
