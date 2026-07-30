using Sportner.Domain.Entities;

namespace Sportner.Domain.Data.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IProfileRepository Profiles { get; }
    IEventRepository Events { get; }
    IEventParticipantRepository EventParticipants { get; }
    IMessageRepository Messages { get; }
    IReviewRepository Reviews { get; }
    ISportRepository Sports { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default);
}

public interface IProfileRepository : IBaseRepository<Profile>
{
}

public interface IEventRepository : IBaseRepository<Event>
{
}

public interface IEventParticipantRepository : IBaseRepository<EventParticipant>
{
}

public interface IMessageRepository : IBaseRepository<Message>
{
}

public interface IReviewRepository : IBaseRepository<Review>
{
}

public interface ISportRepository : IBaseRepository<Sport>
{
    Task<Sport?> FindByStringIdAsync(string id, CancellationToken cancellationToken = default);
}
