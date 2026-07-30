using Sportner.Application.DTOs.Sports;

namespace Sportner.Application.Services;

public interface ISportService
{
    Task<IReadOnlyList<SportDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
