using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Sports;

namespace Sportner.Application.Features.Catalog.Sports.CreateSport;

internal sealed class CreateSportCommandHandler
    : ICommandHandler<CreateSportCommand, SportResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public CreateSportCommandHandler(
        IApplicationDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result<SportResponse>> Handle(
        CreateSportCommand request,
        CancellationToken cancellationToken)
    {
        var utcNow = _timeProvider.GetUtcNow();
        var sport = Sport.Create(
            request.Name,
            request.DisplayOrder,
            utcNow,
            request.Slug,
            request.IconUrl);

        var nameTaken = await _dbContext.Sports
            .AnyAsync(candidate => candidate.Name == sport.Name, cancellationToken);

        if (nameTaken)
        {
            return Result<SportResponse>.Failure(SportErrors.NameTaken);
        }

        var slugTaken = await _dbContext.Sports
            .AnyAsync(candidate => candidate.Slug == sport.Slug, cancellationToken);

        if (slugTaken)
        {
            return Result<SportResponse>.Failure(SportErrors.SlugTaken);
        }

        _dbContext.Sports.Add(sport);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<SportResponse>.Success(ToResponse(sport));
    }

    private static SportResponse ToResponse(Sport sport) =>
        new(sport.Id, sport.Name, sport.Slug, sport.IconUrl, sport.DisplayOrder);
}
