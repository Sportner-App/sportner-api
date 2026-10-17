using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Events;

/// <summary>
/// Etkinlik sorusunda ya da yanıtında etiketlenen kullanıcı. Gönderi, yorum ve
/// mesaj etiketleriyle aynı gerekçe: kullanıcı adları değişebildiği için etiket
/// metinle değil kimlikle saklanıyor.
/// </summary>
public class EventQuestionMention : AuditableEntity
{
    private EventQuestionMention()
    {
    }

    public Guid QuestionId { get; private set; }

    public Guid UserId { get; private set; }

    /// <summary>Etiket yazıldığı andaki kullanıcı adı.</summary>
    public string Username { get; private set; } = null!;

    public static EventQuestionMention Create(
        Guid questionId,
        Guid userId,
        string username,
        DateTimeOffset utcNow)
    {
        if (questionId == Guid.Empty)
        {
            throw new DomainException("Question id is required.");
        }

        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new DomainException("Username is required.");
        }

        return new EventQuestionMention
        {
            Id = Guid.NewGuid(),
            QuestionId = questionId,
            UserId = userId,
            Username = username.Trim(),
            CreatedAt = utcNow
        };
    }
}
