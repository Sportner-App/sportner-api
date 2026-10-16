using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;

namespace Sportner.Application.Features.Social.Posts;

/// <summary>
/// Gönderide etiketlenecek kullanıcıları doğrular ve kullanıcı adlarını çözer.
///
/// Etiketleme arkadaşlarla sınırlı: herkesin etiketlenebilmesi, tanımadığı
/// kişilerin gönderilerinde adı geçen kullanıcıya bildirim yağdırmanın kolay
/// bir yolu olurdu. Sınırı genişletmek ileride kolay, daraltmak değil.
/// </summary>
internal static class PostMentionResolver
{
    internal static async Task<Result<IReadOnlyList<(Guid UserId, string Username)>>> ResolveAsync(
        IApplicationDbContext dbContext,
        Guid authorUserId,
        IReadOnlyList<Guid>? mentionedUserIds,
        CancellationToken cancellationToken)
    {
        if (mentionedUserIds is null || mentionedUserIds.Count == 0)
        {
            return Result<IReadOnlyList<(Guid, string)>>.Success([]);
        }

        var requested = mentionedUserIds
            .Where(id => id != Guid.Empty && id != authorUserId)
            .Distinct()
            .ToList();

        if (requested.Count == 0)
        {
            return Result<IReadOnlyList<(Guid, string)>>.Success([]);
        }

        var friendIds = await SocialQueries.AcceptedFriendIds(dbContext, authorUserId)
            .Where(id => requested.Contains(id))
            .ToListAsync(cancellationToken);

        if (friendIds.Count != requested.Count)
        {
            return Result<IReadOnlyList<(Guid, string)>>.Failure(PostErrors.MentionNotAllowed);
        }

        // Kullanıcı adı etiketle birlikte saklanıyor: içerikteki "@ahmet"
        // yazısını etikete bağlayabilmek için yazıldığı andaki hâli gerekiyor.
        var usernames = await dbContext.UserProfiles.AsNoTracking()
            .Where(profile => friendIds.Contains(profile.UserId))
            .Select(profile => new { profile.UserId, profile.Username })
            .ToListAsync(cancellationToken);

        if (usernames.Count != friendIds.Count)
        {
            return Result<IReadOnlyList<(Guid, string)>>.Failure(PostErrors.MentionNotAllowed);
        }

        return Result<IReadOnlyList<(Guid, string)>>.Success(
            usernames
                .Select(row => (row.UserId, row.Username))
                .ToList());
    }
}
