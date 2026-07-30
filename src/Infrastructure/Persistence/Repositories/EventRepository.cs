using Sportner.Domain.Data.Interfaces;
using Sportner.Domain.Entities;

namespace Sportner.Infrastructure.Persistence.Repositories;

public class EventRepository(AppDbContext context)
    : BaseRepository<Event>(context), IEventRepository;
