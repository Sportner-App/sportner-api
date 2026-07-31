using Sportner.Domain.Data.Interfaces;
using Sportner.Domain.Entities;

namespace Sportner.Infrastructure.Persistence.Repositories;

public class MessageRepository(SportnerDbContext context)
    : BaseRepository<Message>(context), IMessageRepository;
