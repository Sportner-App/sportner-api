using Sportner.Domain.Data.Interfaces;
using Sportner.Domain.Entities;

namespace Sportner.Infrastructure.Persistence.Repositories;

public class ProfileRepository(AppDbContext context)
    : BaseRepository<Profile>(context), IProfileRepository;
