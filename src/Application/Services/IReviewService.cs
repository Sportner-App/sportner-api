using Sportner.Application.DTOs.Reviews;

namespace Sportner.Application.Services;

public interface IReviewService
{
    Task<ReviewDto> CreateAsync(CreateReviewDto dto, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReviewDto>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
