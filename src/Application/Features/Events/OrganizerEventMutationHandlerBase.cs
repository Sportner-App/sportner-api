using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using DomainEvent = Sportner.Domain.Events.Event;

namespace Sportner.Application.Features.Events;

/// <summary>
/// Shared flow for organizer-only mutations that return the refreshed event detail.
/// </summary>
internal abstract class OrganizerEventMutationHandlerBase
{
    protected OrganizerEventMutationHandlerBase(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        DbContext = dbContext;
        CurrentUser = currentUser;
        TimeProvider = timeProvider;
    }

    protected IApplicationDbContext DbContext { get; }

    protected ICurrentUser CurrentUser { get; }

    protected TimeProvider TimeProvider { get; }

    protected async Task<Result<EventResponse>> MutateAsync(
        Guid eventId,
        Func<DomainEvent, DateTimeOffset, CancellationToken, Task<Result>> mutate,
        CancellationToken cancellationToken)
    {
        if (CurrentUser.UserId is not { } userId)
        {
            return Result<EventResponse>.Failure(EventErrors.NotAuthenticated);
        }

        var loaded = await EventAccess.LoadOrganizerEventAsync(
            DbContext,
            userId,
            eventId,
            cancellationToken);

        if (loaded.IsFailure)
        {
            return Result<EventResponse>.Failure(loaded.Errors);
        }

        var (_, @event) = loaded.Value!;
        var mutation = await mutate(@event, TimeProvider.GetUtcNow(), cancellationToken);

        if (mutation.IsFailure)
        {
            return Result<EventResponse>.Failure(mutation.Errors);
        }

        await DbContext.SaveChangesAsync(cancellationToken);

        var response = await EventQueries.GetDetailAsync(DbContext, eventId, userId, cancellationToken);

        return Result<EventResponse>.Success(response!);
    }

    protected async Task<Result<EventResponse>> MutateAsync(
        Guid eventId,
        Action<DomainEvent, DateTimeOffset> mutate,
        CancellationToken cancellationToken) =>
        await MutateAsync(
            eventId,
            (@event, utcNow, _) =>
            {
                mutate(@event, utcNow);
                return Task.FromResult(Result.Success());
            },
            cancellationToken);
}
