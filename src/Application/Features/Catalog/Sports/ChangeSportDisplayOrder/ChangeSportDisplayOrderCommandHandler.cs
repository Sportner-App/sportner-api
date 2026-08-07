using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Sports;

namespace Sportner.Application.Features.Catalog.Sports.ChangeSportDisplayOrder;

internal sealed class ChangeSportDisplayOrderCommandHandler
    : ICommandHandler<ChangeSportDisplayOrderCommand, SportResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public ChangeSportDisplayOrderCommandHandler(
        IApplicationDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result<SportResponse>> Handle(
        ChangeSportDisplayOrderCommand request,
        CancellationToken cancellationToken)
    {
        var sport = await _dbContext.Sports
            .FirstOrDefaultAsync(candidate => candidate.Id == request.SportId, cancellationToken);

        if (sport is null)
        {
            return Result<SportResponse>.Failure(SportErrors.NotFound);
        }

        sport.ChangeDisplayOrder(request.DisplayOrder, _timeProvider.GetUtcNow());
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<SportResponse>.Success(ToResponse(sport));
    }

    private static SportResponse ToResponse(Sport sport) =>
        new(sport.Id, sport.Name, sport.Slug, sport.IconUrl, sport.DisplayOrder);
}
