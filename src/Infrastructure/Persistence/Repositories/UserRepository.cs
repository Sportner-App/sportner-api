using Sportner.Domain.Data.Interfaces;
using Sportner.Domain.Entities;

namespace Sportner.Infrastructure.Persistence.Repositories;

public class UserRepository(SportnerDbContext context)
    : BaseRepository<User>(context), IUserRepository;
