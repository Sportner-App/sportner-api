using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Sportner.Domain.Data.Interfaces;

namespace Sportner.Infrastructure.Persistence.Repositories;

public class BaseRepository<TEntity> : IBaseRepository<TEntity> where TEntity : class
{
    protected readonly AppDbContext Context;
    protected readonly DbSet<TEntity> DbSet;

    public BaseRepository(AppDbContext context)
    {
        Context = context;
        DbSet = context.Set<TEntity>();
    }

    public IQueryable<TEntity> AsQueryable() => DbSet.AsQueryable();

    public IQueryable<TEntity> AsQueryable(Expression<Func<TEntity, bool>> predicate) =>
        DbSet.Where(predicate);

    public async Task InsertOneAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        await DbSet.AddAsync(entity, cancellationToken);

    public async Task InsertManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default) =>
        await DbSet.AddRangeAsync(entities, cancellationToken);

    public void UpdateOne(TEntity entity) => DbSet.Update(entity);

    public void DeleteOne(TEntity entity) => DbSet.Remove(entity);

    public Task<TEntity?> FindOneAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        DbSet.FirstOrDefaultAsync(predicate, cancellationToken);

    public async Task<TEntity?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await DbSet.FindAsync([id], cancellationToken);

    public Task<long> CountAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        DbSet.LongCountAsync(predicate, cancellationToken);

    public Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        DbSet.AnyAsync(predicate, cancellationToken);
}
