using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace EfCoreWrapper.Data.Repository;

/// <summary>
/// Defines generic data access operations for an entity type.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface IRepository<T>
    where T : class
{
    /// <summary>
    /// Adds a new entity to the current unit of work.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task CreateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple entities to the current unit of work.
    /// </summary>
    /// <param name="entities">The entities to add.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task CreateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an entity as modified.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks multiple entities as modified.
    /// </summary>
    /// <param name="entities">The entities to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an entity from the current unit of work.
    /// </summary>
    /// <param name="entity">The entity to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes multiple entities from the current unit of work.
    /// </summary>
    /// <param name="entities">The entities to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an entity by its primary key.
    /// </summary>
    /// <param name="id">The primary key value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task DeleteByIdAsync(object id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an entity by its primary key, optionally applying eager-loading.
    /// </summary>
    /// <param name="id">The primary key value.</param>
    /// <param name="include">A query shaper for eager-loading related data.</param>
    /// <param name="asNoTracking">Whether to use no-tracking queries.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The entity when found; otherwise <see langword="null"/>.</returns>
    Task<T?> GetByIdAsync(
        object id,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entities with optional filtering, sorting, eager-loading, and pagination.
    /// </summary>
    /// <param name="predicate">An optional filter predicate.</param>
    /// <param name="orderBy">An optional sort definition.</param>
    /// <param name="pageNumber">The 1-based page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="include">A query shaper for eager-loading related data.</param>
    /// <param name="asNoTracking">Whether to use no-tracking queries.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching entities.</returns>
    Task<IReadOnlyList<T>> GetAllAsync(
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        int? pageNumber = null,
        int? pageSize = null,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds entities using a required predicate with optional sorting, eager-loading, and pagination.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <param name="orderBy">An optional sort definition.</param>
    /// <param name="pageNumber">The 1-based page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="include">A query shaper for eager-loading related data.</param>
    /// <param name="asNoTracking">Whether to use no-tracking queries.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching entities.</returns>
    Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        int? pageNumber = null,
        int? pageSize = null,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a raw SQL command.
    /// </summary>
    /// <param name="sql">The SQL command text.</param>
    /// <param name="parameters">The SQL parameters.</param>
    /// <returns>The number of rows affected.</returns>
    Task<int> ExecuteSqlAsync(string sql, params object[] parameters);

    /// <summary>
    /// Executes a raw SQL command.
    /// </summary>
    /// <param name="sql">The SQL command text.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="parameters">The SQL parameters.</param>
    /// <returns>The number of rows affected.</returns>
    Task<int> ExecuteSqlAsync(string sql, CancellationToken cancellationToken, params object[] parameters);

    /// <summary>
    /// Persists changes to the database.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of state entries written.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a database transaction.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The active transaction.</returns>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current database transaction.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current database transaction.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
