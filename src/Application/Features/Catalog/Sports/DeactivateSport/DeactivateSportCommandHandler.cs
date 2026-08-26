using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Catalog.Sports.DeactivateSport;

internal sealed class DeactivateSportCommandHandler
    : ICommandHandler<DeactivateSportCommand, SportResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public DeactivateSportCommandHandler(
        IApplicationDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result<SportResponse>> Handle(
        DeactivateSportCommand request,
        CancellationToken cancellationToken)
    {
        var sport = await _dbContext.Sports
            .FirstOrDefaultAsync(candidate => candidate.Id == request.SportId, cancellationToken);

        if (sport is null)
        {
            return Result<SportResponse>.Failure(SportErrors.NotFound);
        }

        sport.Deactivate(_timeProvider.GetUtcNow());
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<SportResponse>.Success(SportResponse.From(sport));
    }
}
