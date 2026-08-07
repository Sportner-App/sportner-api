using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.UserSports.RemoveSport;

internal sealed class RemoveSportCommandHandler
    : UserSportMutationHandlerBase,
        ICommandHandler<RemoveSportCommand, IReadOnlyList<UserSportResponse>>
{
    public RemoveSportCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
        : base(dbContext, currentUser, timeProvider)
    {
    }

    public Task<Result<IReadOnlyList<UserSportResponse>>> Handle(
        RemoveSportCommand request,
        CancellationToken cancellationToken) =>
        MutateAsync(
            request.SportId,
            (user, utcNow) => user.RemoveSport(request.SportId, utcNow),
            cancellationToken);
}
