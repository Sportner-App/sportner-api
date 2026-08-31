using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;

namespace Sportner.Application.Features.Social;

internal static class BlockQueries
{
    internal static IQueryable<Guid> BlockedUserIds(
        IApplicationDbContext dbContext,
        Guid userId) =>
        dbContext.UserBlocks.AsNoTracking()
            .Where(block => block.BlockerUserId == userId || block.BlockedUserId == userId)
            .Select(block => block.BlockerUserId == userId
                ? block.BlockedUserId
                : block.BlockerUserId);

    internal static IQueryable<Guid> IdsIBlocked(
        IApplicationDbContext dbContext,
        Guid blockerUserId) =>
        dbContext.UserBlocks.AsNoTracking()
            .Where(block => block.BlockerUserId == blockerUserId)
            .Select(block => block.BlockedUserId);

    internal static Task<bool> BlockedPairExistsAsync(
        IApplicationDbContext dbContext,
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken cancellationToken) =>
        dbContext.UserBlocks.AsNoTracking()
            .AnyAsync(
                block =>
                    (block.BlockerUserId == firstUserId && block.BlockedUserId == secondUserId)
                    || (block.BlockerUserId == secondUserId && block.BlockedUserId == firstUserId),
                cancellationToken);
}
