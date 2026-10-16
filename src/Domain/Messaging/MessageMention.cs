using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Messaging;

/// <summary>
/// Bir mesajda etiketlenen kullanıcı. Gönderi ve yorum etiketleriyle aynı
/// gerekçe: kullanıcı adları değişebildiği için etiket metinle değil kimlikle
/// saklanıyor.
/// </summary>
public class MessageMention : AuditableEntity
{
    private MessageMention()
    {
    }

    public Guid MessageId { get; private set; }

    public Guid UserId { get; private set; }

    /// <summary>Etiket yazıldığı andaki kullanıcı adı.</summary>
    public string Username { get; private set; } = null!;

    public static MessageMention Create(
        Guid messageId,
        Guid userId,
        string username,
        DateTimeOffset utcNow)
    {
        if (messageId == Guid.Empty)
        {
            throw new DomainException("Message id is required.");
        }

        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new DomainException("Username is required.");
        }

        return new MessageMention
        {
            Id = Guid.NewGuid(),
            MessageId = messageId,
            UserId = userId,
            Username = username.Trim(),
            CreatedAt = utcNow
        };
    }
}
