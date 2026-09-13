using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.Auth.UpdatePreferredLanguage;

internal sealed class UpdatePreferredLanguageCommandHandler
    : ICommandHandler<UpdatePreferredLanguageCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public UpdatePreferredLanguageCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(
        UpdatePreferredLanguageCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure(AuthErrors.InvalidRefreshToken);
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(AuthErrors.InvalidRefreshToken);
        }

        user.SetPreferredLanguage(request.Language, _timeProvider.GetUtcNow());

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
