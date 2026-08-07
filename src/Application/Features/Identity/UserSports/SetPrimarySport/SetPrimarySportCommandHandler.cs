using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.UserSports.SetPrimarySport;

internal sealed class SetPrimarySportCommandHandler
    : UserSportMutationHandlerBase,
        ICommandHandler<SetPrimarySportCommand, IReadOnlyList<UserSportResponse>>
{
    public SetPrimarySportCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
        : base(dbContext, currentUser, timeProvider)
    {
    }

    public Task<Result<IReadOnlyList<UserSportResponse>>> Handle(
        SetPrimarySportCommand request,
        CancellationToken cancellationToken) =>
        MutateAsync(
            request.SportId,
            (user, utcNow) => user.SetPrimarySport(request.SportId, utcNow),
            cancellationToken);
}
