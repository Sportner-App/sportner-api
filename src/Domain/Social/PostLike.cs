using Sportner.Domain.Common.Base;
using Sportner.Domain.Common.Exceptions;

namespace Sportner.Domain.Social;

public class PostLike : AggregateRoot
{
    private PostLike()
    {
    }

    public Guid PostId { get; private set; }

    public Guid UserId { get; private set; }

    public static PostLike Create(Guid postId, Guid userId, DateTimeOffset utcNow)
    {
        if (postId == Guid.Empty)
        {
            throw new DomainException("Post id is required.");
        }

        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        return new PostLike
        {
            Id = Guid.NewGuid(),
            PostId = postId,
            UserId = userId,
            CreatedAt = utcNow
        };
    }
}
