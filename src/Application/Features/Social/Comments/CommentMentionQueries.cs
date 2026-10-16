using Microsoft.EntityFrameworkCore;
using Sportner.Application.Abstractions.Persistence;

namespace Sportner.Application.Features.Social.Comments;

internal static class CommentMentionQueries
{
    /// <summary>
    /// Sayfadaki yorumların etiketlerini tek sorguda çekip yanıtlara ekler.
    ///
    /// Liste uçları yanıtı SQL yansıtmasında kuruyor ve oraya bir koleksiyon
    /// sığmıyor; yorum başına ayrı sorgu da N+1 olurdu.
    /// </summary>
    internal static async Task<List<CommentResponse>> AttachMentionsAsync(
        IApplicationDbContext dbContext,
        List<CommentResponse> comments,
        CancellationToken cancellationToken)
    {
        if (comments.Count == 0)
        {
            return comments;
        }

        var commentIds = comments.Select(comment => comment.Id).ToList();

        var mentions = await dbContext.CommentMentions.AsNoTracking()
            .Where(mention => commentIds.Contains(mention.CommentId))
            .Select(mention => new
            {
                mention.CommentId,
                mention.UserId,
                mention.Username,
            })
            .ToListAsync(cancellationToken);

        if (mentions.Count == 0)
        {
            return comments;
        }

        var byComment = mentions
            .GroupBy(mention => mention.CommentId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<PostMentionResponse>)group
                    .Select(mention => new PostMentionResponse(mention.UserId, mention.Username))
                    .ToList());

        return comments
            .Select(comment =>
                byComment.TryGetValue(comment.Id, out var found)
                    ? comment with { Mentions = found }
                    : comment)
            .ToList();
    }
}
