using Microsoft.EntityFrameworkCore;
using Sportner.Application.DTOs.Sports;
using Sportner.Application.Mappers;
using Sportner.Domain.Data.Interfaces;

namespace Sportner.Application.Services;

public class SportService(IUnitOfWork unitOfWork) : ISportService
{
    public async Task<IReadOnlyList<SportDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var sports = await unitOfWork.Sports
            .AsQueryable()
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

        return sports.Select(s => s.ToDto()).ToList();
    }
}
