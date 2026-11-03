using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Application.Features.Identity.UserProfiles;

namespace Sportner.Application.Features.Challenges;

/// <summary>Şu an süren challenge'lar.</summary>
public sealed record ListChallengesQuery : IQuery<IReadOnlyList<ChallengeResponse>>;

internal sealed class ListChallengesQueryHandler
    : IQueryHandler<ListChallengesQuery, IReadOnlyList<ChallengeResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public ListChallengesQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<IReadOnlyList<ChallengeResponse>>> Handle(
        ListChallengesQuery request,
        CancellationToken cancellationToken)
    {
        var utcNow = _timeProvider.GetUtcNow();
        var challenges = await _dbContext.Challenges.AsNoTracking()
            .Where(challenge =>
                challenge.IsActive && challenge.StartsAt <= utcNow && challenge.EndsAt > utcNow)
            .OrderBy(challenge => challenge.SortOrder)
            .ThenBy(challenge => challenge.EndsAt)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<ChallengeResponse>>.Success(
            await ChallengeQueries.BuildResponsesAsync(
                _dbContext, challenges, _currentUser.UserId, cancellationToken));
    }
}

/// <summary>Kullanıcının tamamlayıp kazandığı madalyalar, yeniden eskiye.</summary>
public sealed record ListChallengeMedalsQuery(Guid UserId) : IQuery<IReadOnlyList<ChallengeResponse>>;

internal sealed class ListChallengeMedalsQueryHandler
    : IQueryHandler<ListChallengeMedalsQuery, IReadOnlyList<ChallengeResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public ListChallengeMedalsQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<ChallengeResponse>>> Handle(
        ListChallengeMedalsQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await _dbContext.UserProfiles.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.UserId == request.UserId, cancellationToken);

        if (profile is null)
        {
            return Result<IReadOnlyList<ChallengeResponse>>.Failure(ProfileErrors.NotFound);
        }

        // Gizli ya da engelli profilin madalyaları da profil kadar gizli.
        if (await ProfileQueries.CheckViewAccessAsync(
                _dbContext, profile, _currentUser.UserId, cancellationToken)
            is { } accessError)
        {
            return Result<IReadOnlyList<ChallengeResponse>>.Failure(accessError);
        }

        var completed = await (
                from participant in _dbContext.ChallengeParticipants.AsNoTracking()
                join challenge in _dbContext.Challenges.AsNoTracking()
                    on participant.ChallengeId equals challenge.Id
                where participant.UserId == request.UserId && participant.CompletedAt != null
                orderby participant.CompletedAt descending
                select challenge)
            .ToListAsync(cancellationToken);

        // Madalya rafı: ilerleme hedefle eşit gösterilsin diye kendi kaydıyla oluşturuluyor.
        return Result<IReadOnlyList<ChallengeResponse>>.Success(
            await ChallengeQueries.BuildResponsesAsync(
                _dbContext, completed, request.UserId, cancellationToken));
    }
}
