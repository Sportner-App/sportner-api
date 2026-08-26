using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Catalog.Sports.GetSportBySlug;

internal sealed class GetSportBySlugQueryHandler : IQueryHandler<GetSportBySlugQuery, SportResponse>
{
    private readonly IApplicationDbContext _dbContext;

    public GetSportBySlugQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<SportResponse>> Handle(
        GetSportBySlugQuery request,
        CancellationToken cancellationToken)
    {
        // Slugs are stored lowercase; normalize the lookup so the URL is case-insensitive.
        var slug = request.Slug.Trim().ToLowerInvariant();

        var sport = await _dbContext.Sports.AsNoTracking()
            .Where(candidate => candidate.IsActive && candidate.Slug == slug)
            .Select(candidate => new SportResponse(
                candidate.Id,
                candidate.Name,
                candidate.Slug,
                candidate.IconUrl,
                candidate.CoverImageUrl,
                candidate.DisplayOrder))
            .FirstOrDefaultAsync(cancellationToken);

        return sport is null
            ? Result<SportResponse>.Failure(SportErrors.NotFound)
            : Result<SportResponse>.Success(sport);
    }
}
