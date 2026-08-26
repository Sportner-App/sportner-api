using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Catalog.Sports.RenameSport;

internal sealed class RenameSportCommandHandler
    : ICommandHandler<RenameSportCommand, SportResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public RenameSportCommandHandler(
        IApplicationDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<Result<SportResponse>> Handle(
        RenameSportCommand request,
        CancellationToken cancellationToken)
    {
        var sport = await _dbContext.Sports
            .FirstOrDefaultAsync(candidate => candidate.Id == request.SportId, cancellationToken);

        if (sport is null)
        {
            return Result<SportResponse>.Failure(SportErrors.NotFound);
        }

        var utcNow = _timeProvider.GetUtcNow();
        var previousName = sport.Name;
        var previousSlug = sport.Slug;

        sport.Rename(request.Name, utcNow);

        if (!string.IsNullOrWhiteSpace(request.Slug))
        {
            sport.ChangeSlug(request.Slug, utcNow);
        }

        if (request.IconUrl is not null)
        {
            sport.ChangeIcon(
                string.IsNullOrWhiteSpace(request.IconUrl) ? null : request.IconUrl,
                utcNow);
        }

        if (!string.Equals(previousName, sport.Name, StringComparison.Ordinal))
        {
            var nameTaken = await _dbContext.Sports.AnyAsync(
                candidate => candidate.Id != sport.Id && candidate.Name == sport.Name,
                cancellationToken);

            if (nameTaken)
            {
                return Result<SportResponse>.Failure(SportErrors.NameTaken);
            }
        }

        if (!string.Equals(previousSlug, sport.Slug, StringComparison.Ordinal))
        {
            var slugTaken = await _dbContext.Sports.AnyAsync(
                candidate => candidate.Id != sport.Id && candidate.Slug == sport.Slug,
                cancellationToken);

            if (slugTaken)
            {
                return Result<SportResponse>.Failure(SportErrors.SlugTaken);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<SportResponse>.Success(SportResponse.From(sport));
    }
}
