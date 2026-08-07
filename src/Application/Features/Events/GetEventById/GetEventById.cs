using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Events.GetEventById;

public sealed record GetEventByIdQuery(Guid EventId) : IQuery<EventResponse>;

internal sealed class GetEventByIdQueryHandler : IQueryHandler<GetEventByIdQuery, EventResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetEventByIdQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<EventResponse>> Handle(
        GetEventByIdQuery request,
        CancellationToken cancellationToken)
    {
        var response = await EventQueries.GetDetailAsync(
            _dbContext,
            request.EventId,
            _currentUser.UserId,
            cancellationToken);

        return response is null
            ? Result<EventResponse>.Failure(EventErrors.NotFound)
            : Result<EventResponse>.Success(response);
    }
}
