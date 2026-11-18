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

    /// <summary>App Review / ekip test hesaplarının kullanıcı kimlikleri.</summary>
    internal static IQueryable<Guid> TestAccountUserIds(IApplicationDbContext dbContext) =>
        dbContext.Users.AsNoTracking()
            .Where(user => user.IsTestAccount)
            .Select(user => user.Id);

    /// <summary>
    /// Bakan kişiye gösterilmemesi gereken test hesapları: girişsiz ya da gerçek kullanıcıya
    /// tüm test hesapları, test hesabı olan bakana boş küme. Engel ilişkisi katılmaz; etkinlik ve
    /// kulüp gibi engel kuralı kendine özgü olan okuma yolları yalnızca bunu kullanır.
    /// </summary>
    internal static IQueryable<Guid> HiddenTestAccountIds(
        IApplicationDbContext dbContext,
        Guid? viewerId)
    {
        if (viewerId is not { } viewer)
        {
            return TestAccountUserIds(dbContext);
        }

        var users = dbContext.Users.AsNoTracking();

        return users
            .Where(user => user.IsTestAccount
                && !users.Any(candidate => candidate.Id == viewer && candidate.IsTestAccount))
            .Select(user => user.Id);
    }

    /// <summary>
    /// Bakan kişiye içeriği gösterilmemesi gereken kullanıcılar (okuma/listeleme için):
    /// engel ilişkileri artı <see cref="HiddenTestAccountIds"/>. Test hesabı her şeyi (test içeriği
    /// dahil) görür. Yazma ve yetki kontrolleri <see cref="BlockedUserIds"/> /
    /// <see cref="BlockedPairExistsAsync"/> ile kalır: test hesabının etkileşimi engellenmez.
    /// Tek SQL: UNION ALL; viewer test hesabıysa test kolu boş döner.
    /// </summary>
    internal static IQueryable<Guid> HiddenUserIds(
        IApplicationDbContext dbContext,
        Guid? viewerId)
    {
        if (viewerId is not { } viewer)
        {
            return TestAccountUserIds(dbContext);
        }

        return BlockedUserIds(dbContext, viewer)
            .Concat(HiddenTestAccountIds(dbContext, viewer));
    }

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
