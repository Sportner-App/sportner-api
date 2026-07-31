using Sportner.Domain.Entities;

namespace Sportner.Domain.Data.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IEventRepository Events { get; }
    IUserEventRepository UserEvents { get; }
    IMessageRepository Messages { get; }
    IReviewRepository Reviews { get; }
    ISportRepository Sports { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default);
}

public interface IUserRepository : IBaseRepository<User>
{
}

public interface IEventRepository : IBaseRepository<Event>
{
}

public interface IUserEventRepository : IBaseRepository<UserEvent>
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
