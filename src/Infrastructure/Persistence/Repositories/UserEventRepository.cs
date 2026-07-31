using Sportner.Domain.Data.Interfaces;
using Sportner.Domain.Entities;

namespace Sportner.Infrastructure.Persistence.Repositories;

public class UserEventRepository(SportnerDbContext context)
    : BaseRepository<UserEvent>(context), IUserEventRepository;
