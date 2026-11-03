using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Challenges;

namespace Sportner.Application.Features.Challenges;

public sealed record JoinChallengeCommand(Guid ChallengeId) : ICommand<ChallengeResponse>;

public sealed record LeaveChallengeCommand(Guid ChallengeId) : ICommand<ChallengeResponse>;

internal sealed class JoinChallengeCommandHandler
    : ICommandHandler<JoinChallengeCommand, ChallengeResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public JoinChallengeCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<ChallengeResponse>> Handle(
        JoinChallengeCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<ChallengeResponse>.Failure(ChallengeErrors.NotAuthenticated);
        }

        var challenge = await _dbContext.Challenges.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == request.ChallengeId, cancellationToken);

        if (challenge is null)
        {
            return Result<ChallengeResponse>.Failure(ChallengeErrors.NotFound);
        }

        var utcNow = _timeProvider.GetUtcNow();
        if (!challenge.IsRunning(utcNow))
        {
            return Result<ChallengeResponse>.Failure(ChallengeErrors.NotRunning);
        }

        // Tekrar katılmak hata değil: çift dokunuş ya da yeniden deneme aynı sonucu verir.
        var alreadyJoined = await _dbContext.ChallengeParticipants
            .AnyAsync(
                participant => participant.ChallengeId == challenge.Id && participant.UserId == userId,
                cancellationToken);

        if (!alreadyJoined)
        {
            _dbContext.ChallengeParticipants.Add(ChallengeParticipant.Join(challenge.Id, userId, utcNow));
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return await RespondAsync(_dbContext, challenge, userId, cancellationToken);
    }

    internal static async Task<Result<ChallengeResponse>> RespondAsync(
        IApplicationDbContext dbContext,
        Challenge challenge,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var responses = await ChallengeQueries.BuildResponsesAsync(
            dbContext, [challenge], userId, cancellationToken);
        return Result<ChallengeResponse>.Success(responses[0]);
    }
}

internal sealed class LeaveChallengeCommandHandler
    : ICommandHandler<LeaveChallengeCommand, ChallengeResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public LeaveChallengeCommandHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<ChallengeResponse>> Handle(
        LeaveChallengeCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result<ChallengeResponse>.Failure(ChallengeErrors.NotAuthenticated);
        }

        var challenge = await _dbContext.Challenges.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == request.ChallengeId, cancellationToken);

        if (challenge is null)
        {
            return Result<ChallengeResponse>.Failure(ChallengeErrors.NotFound);
        }

        var participation = await _dbContext.ChallengeParticipants
            .FirstOrDefaultAsync(
                participant => participant.ChallengeId == challenge.Id && participant.UserId == userId,
                cancellationToken);

        if (participation is not null)
        {
            if (participation.CompletedAt is not null)
            {
                return Result<ChallengeResponse>.Failure(ChallengeErrors.AlreadyCompleted);
            }

            _dbContext.ChallengeParticipants.Remove(participation);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return await JoinChallengeCommandHandler.RespondAsync(
            _dbContext, challenge, userId, cancellationToken);
    }
}
