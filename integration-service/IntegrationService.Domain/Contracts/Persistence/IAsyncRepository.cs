using System.Linq.Expressions;
using IntegrationService.Domain.Common;

namespace IntegrationService.Domain.Contracts.Persistence;

/// <summary>
/// Generic repository interface — giống iolis-instrument IAsyncRepository.
/// </summary>
public interface IAsyncRepository<T> where T : EntityBase
{
    Task<IReadOnlyList<T>> GetAllAsync();
    Task<IReadOnlyList<T>> GetAsync(Expression<Func<T, bool>> predicate);
    Task<IReadOnlyList<T>> GetAsync(
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        List<Expression<Func<T, object>>>? includes = null,
        bool disableTracking = true);
    Task<T?> GetByIdAsync(Guid id);
    Task<T?> GetOneAsync(Expression<Func<T, bool>> predicate);
    Task<T> AddAsync(T entity);
    Task AddManyAsync(IReadOnlyList<T> entities);
    Task UpdateAsync(T entity);
    Task UpdateManyAsync(IReadOnlyList<T> entities);
    Task DeleteAsync(T entity);
    Task MultipleDeleteAsync(IReadOnlyList<T> entities);
}
