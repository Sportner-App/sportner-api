using Sportner.Domain.Data.Interfaces;
using Sportner.Infrastructure.Persistence.Repositories;

namespace Sportner.Infrastructure.Persistence;

public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    private IProfileRepository? _profiles;
    private IEventRepository? _events;
    private IEventParticipantRepository? _eventParticipants;
    private IMessageRepository? _messages;
    private IReviewRepository? _reviews;
    private ISportRepository? _sports;

    public IProfileRepository Profiles =>
        _profiles ??= new ProfileRepository(context);

    public IEventRepository Events =>
        _events ??= new EventRepository(context);

    public IEventParticipantRepository EventParticipants =>
        _eventParticipants ??= new EventParticipantRepository(context);

    public IMessageRepository Messages =>
        _messages ??= new MessageRepository(context);

    public IReviewRepository Reviews =>
        _reviews ??= new ReviewRepository(context);

    public ISportRepository Sports =>
        _sports ??= new SportRepository(context);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await operation(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public void Dispose() => context.Dispose();
}
