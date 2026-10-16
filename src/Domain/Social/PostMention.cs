using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Social;

/// <summary>
/// Bir gönderide etiketlenen kullanıcı.
///
/// Etiket metinle değil kimlikle saklanıyor: kullanıcı adları 30 günde bir
/// değişebiliyor (bkz. UserProfile.ChangeUsername), dolayısıyla içerikteki
/// "@ahmet" yazısına güvenilse etiket bir süre sonra kimseyi göstermeyen ölü
/// metne dönerdi.
///
/// <see cref="Username"/> yazıldığı andaki hâli: içerikteki metni etikete
/// bağlayabilmek için gerekiyor. İçerik düz metin olarak saklandığından
/// konum (offset) tutmak yerine jeton eşleştiriliyor; kullanıcı adını sonradan
/// değiştiren biri için metin eski adı gösterir ama bağlantı doğru kişiye gider.
/// </summary>
public class PostMention : AuditableEntity
{
    private PostMention()
    {
    }

    public Guid PostId { get; private set; }

    public Guid UserId { get; private set; }

    /// <summary>Etiket yazıldığı andaki kullanıcı adı.</summary>
    public string Username { get; private set; } = null!;

    public static PostMention Create(
        Guid postId,
        Guid userId,
        string username,
        DateTimeOffset utcNow)
    {
        if (postId == Guid.Empty)
        {
            throw new DomainException("Post id is required.");
        }

        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new DomainException("Username is required.");
        }

        return new PostMention
        {
            Id = Guid.NewGuid(),
            PostId = postId,
            UserId = userId,
            Username = username.Trim(),
            CreatedAt = utcNow
        };
    }
}
