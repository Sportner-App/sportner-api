using Sportner.Domain.Data.Interfaces;
using Sportner.Domain.Entities;

namespace Sportner.Infrastructure.Persistence.Repositories;

public class SportRepository(AppDbContext context)
    : BaseRepository<Sport>(context), ISportRepository
{
    public async Task<Sport?> FindByStringIdAsync(string id, CancellationToken cancellationToken = default) =>
        await DbSet.FindAsync([id], cancellationToken);
}
