using System.Linq.Expressions;
using IntegrationService.Domain.Common;
using IntegrationService.Domain.Contracts.Persistence;
using IntegrationService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IntegrationService.Infrastructure.Repositories;

/// <summary>
/// Generic repository base — giống iolis-instrument RepositoryBase.
/// </summary>
public class RepositoryBase<T> : IAsyncRepository<T> where T : EntityBase
{
    protected readonly IntegrationDbContext _dbContext;

    public RepositoryBase(IntegrationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<IReadOnlyList<T>> GetAllAsync()
        => await _dbContext.Set<T>().ToListAsync();

    public async Task<IReadOnlyList<T>> GetAsync(Expression<Func<T, bool>> predicate)
        => await _dbContext.Set<T>().Where(predicate).ToListAsync();

    public async Task<IReadOnlyList<T>> GetAsync(
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        List<Expression<Func<T, object>>>? includes = null,
        bool disableTracking = true)
    {
        IQueryable<T> query = _dbContext.Set<T>();
        if (disableTracking) query = query.AsNoTracking();
        if (includes != null) query = includes.Aggregate(query, (q, i) => q.Include(i));
        if (predicate != null) query = query.Where(predicate);
        return orderBy != null
            ? await orderBy(query).ToListAsync()
            : await query.ToListAsync();
    }

    public async Task<T?> GetByIdAsync(Guid id)
        => await _dbContext.Set<T>().FindAsync(id);

    public async Task<T?> GetOneAsync(Expression<Func<T, bool>> predicate)
        => await _dbContext.Set<T>().Where(predicate).FirstOrDefaultAsync();

    public async Task<T> AddAsync(T entity)
    {
        _dbContext.Set<T>().Add(entity);
        await _dbContext.SaveChangesAsync();
        return entity;
    }

    public async Task AddManyAsync(IReadOnlyList<T> entities)
    {
        _dbContext.Set<T>().AddRange(entities);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(T entity)
    {
        _dbContext.Entry(entity).State = EntityState.Modified;
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateManyAsync(IReadOnlyList<T> entities)
    {
        foreach (var e in entities)
            _dbContext.Entry(e).State = EntityState.Modified;
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(T entity)
    {
        _dbContext.Set<T>().Remove(entity);
        await _dbContext.SaveChangesAsync();
    }

    public async Task MultipleDeleteAsync(IReadOnlyList<T> entities)
    {
        _dbContext.Set<T>().RemoveRange(entities);
        await _dbContext.SaveChangesAsync();
    }
}
