using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;
using Sportner.Application.Common.Results;
using Sportner.Domain.Messaging;

namespace Sportner.Application.Features.Messaging;

/// <summary>
/// Mesajda etiketlenecek kullanıcıları doğrular ve kullanıcı adlarını çözer.
///
/// Ölçüt arkadaşlık değil <b>sohbet üyeliği</b>: etkinlik grup sohbetlerindeki
/// insanlar birbiriyle arkadaş olmak zorunda değil ve asıl etiketlenmek
/// istenen kişiler tam olarak onlar. Zaten mesajı görebilen birini
/// etiketlemek yeni bir erişim açmıyor.
/// </summary>
internal static class MessageMentionResolver
{
    internal static async Task<Result<IReadOnlyList<(Guid UserId, string Username)>>> ResolveAsync(
        IApplicationDbContext dbContext,
        Conversation conversation,
        Guid senderUserId,
        IReadOnlyList<Guid>? mentionedUserIds,
        CancellationToken cancellationToken)
    {
        if (mentionedUserIds is null || mentionedUserIds.Count == 0)
        {
            return Result<IReadOnlyList<(Guid, string)>>.Success([]);
        }

        var requested = mentionedUserIds
            .Where(id => id != Guid.Empty && id != senderUserId)
            .Distinct()
            .ToList();

        if (requested.Count == 0)
        {
            return Result<IReadOnlyList<(Guid, string)>>.Success([]);
        }

        var memberIds = conversation.Members
            .Where(member => member.IsActive() && requested.Contains(member.UserId))
            .Select(member => member.UserId)
            .ToList();

        if (memberIds.Count != requested.Count)
        {
            return Result<IReadOnlyList<(Guid, string)>>.Failure(
                MessagingErrors.MentionNotAllowed);
        }

        var usernames = await dbContext.UserProfiles.AsNoTracking()
            .Where(profile => memberIds.Contains(profile.UserId))
            .Select(profile => new { profile.UserId, profile.Username })
            .ToListAsync(cancellationToken);

        if (usernames.Count != memberIds.Count)
        {
            return Result<IReadOnlyList<(Guid, string)>>.Failure(
                MessagingErrors.MentionNotAllowed);
        }

        return Result<IReadOnlyList<(Guid, string)>>.Success(
            usernames.Select(row => (row.UserId, row.Username)).ToList());
    }
}
