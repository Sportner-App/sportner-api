using Sportner.Domain.Data.Interfaces;
using Sportner.Domain.Entities;

namespace Sportner.Infrastructure.Persistence.Repositories;

public class EventParticipantRepository(AppDbContext context)
    : BaseRepository<EventParticipant>(context), IEventParticipantRepository;
