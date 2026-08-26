using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Common.Enums;

namespace Sportner.Application.Features.Events.PublishEvent;

public sealed record PublishEventCommand(Guid EventId) : ICommand<EventResponse>;

internal sealed class PublishEventCommandHandler
    : OrganizerEventMutationHandlerBase, ICommandHandler<PublishEventCommand, EventResponse>
{
    public PublishEventCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
        : base(dbContext, currentUser, timeProvider)
    {
    }

    public Task<Result<EventResponse>> Handle(
        PublishEventCommand request,
        CancellationToken cancellationToken) =>
        MutateAsync(
            request.EventId,
            async (@event, utcNow, ct) =>
            {
                var wasDraft = @event.Status == EventStatus.Draft;

                @event.Publish(utcNow);

                await EventAccess.EnsureEventConversationAsync(DbContext, @event, utcNow, ct);

                if (wasDraft && @event.Status is EventStatus.Published or EventStatus.Full)
                {
                    foreach (var participant in @event.Participants.Where(item =>
                                 item.UserId is { } userId
                                 && userId != @event.OrganizerUserId
                                 && item.Status is ParticipantStatus.Approved
                                     or ParticipantStatus.Attended))
                    {
                        await EventAccess.AddConversationMemberIfPresentAsync(
                            DbContext,
                            @event.Id,
                            participant.UserId!.Value,
                            utcNow,
                            ct);
                    }

                    var statistics = await DbContext.UserStatistics
                        .FirstOrDefaultAsync(
                            candidate => candidate.UserId == @event.OrganizerUserId,
                            ct);

                    statistics?.IncreaseHostedEvents(utcNow);
                }

                return Result.Success();
            },
            cancellationToken);
}
