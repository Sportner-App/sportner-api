using System.Linq.Expressions;

namespace Sportner.Domain.Data.Interfaces;

public interface IBaseRepository<TEntity> where TEntity : class
{
    IQueryable<TEntity> AsQueryable();
    IQueryable<TEntity> AsQueryable(Expression<Func<TEntity, bool>> predicate);
    Task InsertOneAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task InsertManyAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);
    void UpdateOne(TEntity entity);
    void DeleteOne(TEntity entity);
    Task<TEntity?> FindOneAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    Task<TEntity?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<long> CountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
}
