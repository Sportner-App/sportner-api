using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Social;

/// <summary>
/// Bir yorumda etiketlenen kullanıcı. <see cref="PostMention"/> ile aynı
/// gerekçelere dayanıyor: etiket metinle değil kimlikle saklanıyor, çünkü
/// kullanıcı adları 30 günde bir değişebiliyor.
///
/// Gönderi ve yorum etiketleri ayrı tablolarda: tek tabloda toplamak
/// PostId/CommentId'den birini boş bırakmayı ve iki tarafa da temiz yabancı
/// anahtar koyamamayı gerektirirdi.
/// </summary>
public class CommentMention : AuditableEntity
{
    private CommentMention()
    {
    }

    public Guid CommentId { get; private set; }

    public Guid UserId { get; private set; }

    /// <summary>Etiket yazıldığı andaki kullanıcı adı.</summary>
    public string Username { get; private set; } = null!;

    public static CommentMention Create(
        Guid commentId,
        Guid userId,
        string username,
        DateTimeOffset utcNow)
    {
        if (commentId == Guid.Empty)
        {
            throw new DomainException("Comment id is required.");
        }

        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new DomainException("Username is required.");
        }

        return new CommentMention
        {
            Id = Guid.NewGuid(),
            CommentId = commentId,
            UserId = userId,
            Username = username.Trim(),
            CreatedAt = utcNow
        };
    }
}
