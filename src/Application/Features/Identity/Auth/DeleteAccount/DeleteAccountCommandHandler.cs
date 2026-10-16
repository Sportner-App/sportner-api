using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Identity.Auth.DeleteAccount;

internal sealed class DeleteAccountCommandHandler : ICommandHandler<DeleteAccountCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public DeleteAccountCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure(AuthErrors.InvalidRefreshToken);
        }

        // Her ikisi de yedek alan (backing field) üzerinden eşlenen koleksiyon:
        // Include edilmezse User.Delete() boş liste üzerinde dönüyor, oturumlar
        // iptal edilmiyor ve sosyal hesap bağlantıları silinmeden kalıyordu.
        var user = await _dbContext.Users
            .Include(candidate => candidate.Sessions)
            .Include(candidate => candidate.ExternalLogins)
            .FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(AuthErrors.InvalidRefreshToken);
        }

        user.Delete(_timeProvider.GetUtcNow());

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
