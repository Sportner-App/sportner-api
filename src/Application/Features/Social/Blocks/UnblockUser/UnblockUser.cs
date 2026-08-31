using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Authentication;
using Sportner.Application.Abstractions.Messaging;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Social.Blocks.UnblockUser;

public sealed record UnblockUserCommand(Guid UserId) : ICommand;

internal sealed class UnblockUserCommandHandler : ICommandHandler<UnblockUserCommand>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public UnblockUserCommandHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(UnblockUserCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } blockerId)
        {
            return Result.Failure(BlockErrors.NotAuthenticated);
        }

        var block = await _dbContext.UserBlocks
            .FirstOrDefaultAsync(
                candidate =>
                    candidate.BlockerUserId == blockerId && candidate.BlockedUserId == request.UserId,
                cancellationToken);

        if (block is not null)
        {
            _dbContext.UserBlocks.Remove(block);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
