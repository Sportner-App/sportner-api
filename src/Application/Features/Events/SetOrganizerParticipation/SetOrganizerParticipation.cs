using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Events.SetOrganizerParticipation;

/// <summary>
/// Kulüp etkinliğinde oluşturan kişinin oyuncu olarak da katılıp katılmayacağını değiştirir.
/// Kişisel bir tercih olduğu için yalnızca oluşturan çağırabilir; kulüp yöneticileri çağıramaz.
/// </summary>
public sealed record SetOrganizerParticipationCommand(Guid EventId, bool Participates)
    : ICommand<EventResponse>;

internal sealed class SetOrganizerParticipationCommandHandler
    : OrganizerEventMutationHandlerBase, ICommandHandler<SetOrganizerParticipationCommand, EventResponse>
{
    public SetOrganizerParticipationCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
        : base(dbContext, currentUser, timeProvider)
    {
    }

    protected override Task<Result<(Domain.Users.User User, Domain.Events.Event Event)>> LoadEventAsync(
        Guid userId,
        Guid eventId,
        CancellationToken cancellationToken) =>
        EventAccess.LoadCreatorEventAsync(DbContext, userId, eventId, cancellationToken);

    public Task<Result<EventResponse>> Handle(
        SetOrganizerParticipationCommand request,
        CancellationToken cancellationToken) =>
        MutateAsync(
            request.EventId,
            (@event, utcNow, _) => Task.FromResult(Apply(request, @event, utcNow)),
            cancellationToken);

    private Result Apply(
        SetOrganizerParticipationCommand request,
        Domain.Events.Event @event,
        DateTimeOffset utcNow)
    {
        if (@event.OrganizationId is null)
        {
            return Result.Failure(EventErrors.OrganizerParticipationClubOnly);
        }

        if (@event.HasEnded(utcNow))
        {
            return Result.Failure(EventErrors.ParticipationLocked);
        }

        // Zaten istenen durumdaysa no-op; kontenjan yalnızca gerçekten katılırken aranır.
        if (request.Participates
            && !@event.IsOrganizerParticipant
            && !@event.HasAvailableCapacity())
        {
            return Result.Failure(EventErrors.CapacityFull);
        }

        var added = @event.SetOrganizerParticipation(request.Participates, utcNow);

        // İstemci üretimli Guid'li yeni satırı EF Modified sanmasın; insert zorla.
        if (added is not null)
        {
            DbContext.MarkAsAdded(added);
        }

        return Result.Success();
    }
}
