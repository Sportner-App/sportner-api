namespace Sportner.Application.Abstractions.Recommendations;

public interface IRecommendationService
{
    Task<IReadOnlyList<Scored<RecommendedPerson>>> ScorePeopleAsync(
        Guid viewerUserId,
        int limit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Scored<RecommendedEvent>>> ScoreEventsAsync(
        Guid viewerUserId,
        EventRecommendationRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Scored<RecommendedPost>>> ScorePostsAsync(
        Guid viewerUserId,
        int limit,
        CancellationToken cancellationToken = default);
}
