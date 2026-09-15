using System.Linq.Expressions;
using EfCoreWrapper.Data.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace EfCoreWrapper.Data.Repository;

/// <summary>
/// Provides EF Core-backed data access operations for an entity type.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public class Repository<T> : IRepository<T>
    where T : class
{
    private readonly AppDbContext _context;
    private readonly DbSet<T> _dbSet;
    private readonly ILogger<Repository<T>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="Repository{T}"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">The logger.</param>
    public Repository(AppDbContext context, ILogger<Repository<T>> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dbSet = _context.Set<T>();
    }

    /// <inheritdoc />
    public async Task CreateAsync(T entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        try
        {
            await _dbSet.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to create entity of type {EntityType}.", typeof(T).Name);
            throw new RepositoryException($"Failed to create entity of type {typeof(T).Name}.", exception);
        }
    }

    /// <inheritdoc />
    public async Task CreateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        var entityList = ValidateEntities(entities);

        try
        {
            await _dbSet.AddRangeAsync(entityList, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to create entities of type {EntityType}.", typeof(T).Name);
            throw new RepositoryException($"Failed to create entities of type {typeof(T).Name}.", exception);
        }
    }

    /// <inheritdoc />
    public Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            _dbSet.Update(entity);
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to update entity of type {EntityType}.", typeof(T).Name);
            throw new RepositoryException($"Failed to update entity of type {typeof(T).Name}.", exception);
        }
    }

    /// <inheritdoc />
    public Task UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        var entityList = ValidateEntities(entities);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            _dbSet.UpdateRange(entityList);
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to update entities of type {EntityType}.", typeof(T).Name);
            throw new RepositoryException($"Failed to update entities of type {typeof(T).Name}.", exception);
        }
    }

    /// <inheritdoc />
    public Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            _dbSet.Remove(entity);
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to delete entity of type {EntityType}.", typeof(T).Name);
            throw new RepositoryException($"Failed to delete entity of type {typeof(T).Name}.", exception);
        }
    }

    /// <inheritdoc />
    public Task DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        var entityList = ValidateEntities(entities);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            _dbSet.RemoveRange(entityList);
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to delete entities of type {EntityType}.", typeof(T).Name);
            throw new RepositoryException($"Failed to delete entities of type {typeof(T).Name}.", exception);
        }
    }

    /// <inheritdoc />
    public async Task DeleteByIdAsync(object id, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);

        try
        {
            var entity = await _dbSet.FindAsync(new[] { id }, cancellationToken).ConfigureAwait(false);
            if (entity is null)
            {
                _logger.LogWarning("No entity of type {EntityType} was found for id {EntityId}.", typeof(T).Name, id);
                return;
            }

            _dbSet.Remove(entity);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to delete entity of type {EntityType} by id {EntityId}.", typeof(T).Name, id);
            throw new RepositoryException($"Failed to delete entity of type {typeof(T).Name} by id.", exception);
        }
    }

    /// <inheritdoc />
    public async Task<T?> GetByIdAsync(
        object id,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);

        try
        {
            if (include is null && !asNoTracking)
            {
                return await _dbSet.FindAsync(new[] { id }, cancellationToken).ConfigureAwait(false);
            }

            var entityType = _context.Model.FindEntityType(typeof(T))
                ?? throw new RepositoryException($"Entity type {typeof(T).Name} is not mapped in the DbContext.");
            var primaryKey = entityType.FindPrimaryKey()
                ?? throw new RepositoryException($"Entity type {typeof(T).Name} does not define a primary key.");

            if (primaryKey.Properties.Count != 1)
            {
                throw new RepositoryException($"Entity type {typeof(T).Name} must have a single-column primary key for GetByIdAsync.");
            }

            var keyProperty = primaryKey.Properties[0];
            var query = ApplyIncludeAndTracking(_dbSet, include, asNoTracking);
            var parameter = Expression.Parameter(typeof(T), "entity");
            var property = Expression.Property(parameter, keyProperty.Name);
            var convertedId = ConvertIdValue(id, property.Type);
            var constant = Expression.Constant(convertedId, property.Type);
            var predicate = Expression.Lambda<Func<T, bool>>(Expression.Equal(property, constant), parameter);

            return await query.FirstOrDefaultAsync(predicate, cancellationToken).ConfigureAwait(false);
        }
        catch (RepositoryException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to fetch entity of type {EntityType} by id {EntityId}.", typeof(T).Name, id);
            throw new RepositoryException($"Failed to fetch entity of type {typeof(T).Name} by id.", exception);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<T>> GetAllAsync(
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        int? pageNumber = null,
        int? pageSize = null,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await BuildQuery(predicate, orderBy, pageNumber, pageSize, include, asNoTracking)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (RepositoryException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to fetch entities of type {EntityType}.", typeof(T).Name);
            throw new RepositoryException($"Failed to fetch entities of type {typeof(T).Name}.", exception);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        int? pageNumber = null,
        int? pageSize = null,
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool asNoTracking = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        try
        {
            return await BuildQuery(predicate, orderBy, pageNumber, pageSize, include, asNoTracking)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (RepositoryException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to find entities of type {EntityType}.", typeof(T).Name);
            throw new RepositoryException($"Failed to find entities of type {typeof(T).Name}.", exception);
        }
    }

    /// <inheritdoc />
    public async Task<int> ExecuteSqlAsync(string sql, params object[] parameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        try
        {
            return await _context.Database.ExecuteSqlRawAsync(sql, parameters ?? Array.Empty<object>())
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to execute raw SQL for entity type {EntityType}.", typeof(T).Name);
            throw new RepositoryException($"Failed to execute raw SQL for entity type {typeof(T).Name}.", exception);
        }
    }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to save changes for entity type {EntityType}.", typeof(T).Name);
            throw new RepositoryException($"Failed to save changes for entity type {typeof(T).Name}.", exception);
        }
    }

    /// <inheritdoc />
    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to begin transaction for entity type {EntityType}.", typeof(T).Name);
            throw new RepositoryException($"Failed to begin transaction for entity type {typeof(T).Name}.", exception);
        }
    }

    /// <inheritdoc />
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var transaction = _context.Database.CurrentTransaction
                ?? throw new RepositoryException("There is no active transaction to commit.");
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            await transaction.DisposeAsync().ConfigureAwait(false);
        }
        catch (RepositoryException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to commit transaction for entity type {EntityType}.", typeof(T).Name);
            throw new RepositoryException($"Failed to commit transaction for entity type {typeof(T).Name}.", exception);
        }
    }

    /// <inheritdoc />
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var transaction = _context.Database.CurrentTransaction
                ?? throw new RepositoryException("There is no active transaction to roll back.");
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            await transaction.DisposeAsync().ConfigureAwait(false);
        }
        catch (RepositoryException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to roll back transaction for entity type {EntityType}.", typeof(T).Name);
            throw new RepositoryException($"Failed to roll back transaction for entity type {typeof(T).Name}.", exception);
        }
    }

    private IQueryable<T> BuildQuery(
        Expression<Func<T, bool>>? predicate,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy,
        int? pageNumber,
        int? pageSize,
        Func<IQueryable<T>, IQueryable<T>>? include,
        bool asNoTracking)
    {
        ValidatePagination(pageNumber, pageSize);

        IQueryable<T> query = ApplyIncludeAndTracking(_dbSet, include, asNoTracking);

        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        if (orderBy is not null)
        {
            query = orderBy(query);
        }

        if (pageNumber.HasValue && pageSize.HasValue)
        {
            query = query.Skip((pageNumber.Value - 1) * pageSize.Value).Take(pageSize.Value);
        }

        return query;
    }

    private static IQueryable<T> ApplyIncludeAndTracking(
        IQueryable<T> query,
        Func<IQueryable<T>, IQueryable<T>>? include,
        bool asNoTracking)
    {
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return include is null ? query : include(query);
    }

    private static List<T> ValidateEntities(IEnumerable<T> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        var entityList = entities.ToList();
        if (entityList.Any(entity => entity is null))
        {
            throw new ArgumentException("The entity collection cannot contain null values.", nameof(entities));
        }

        return entityList;
    }

    private static void ValidatePagination(int? pageNumber, int? pageSize)
    {
        if (pageNumber.HasValue != pageSize.HasValue)
        {
            throw new RepositoryException("Both pageNumber and pageSize must be provided together.");
        }

        if (pageNumber is <= 0)
        {
            throw new RepositoryException("pageNumber must be greater than zero.");
        }

        if (pageSize is <= 0)
        {
            throw new RepositoryException("pageSize must be greater than zero.");
        }
    }

    private static object ConvertIdValue(object id, Type targetType)
    {
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlyingType.IsInstanceOfType(id))
        {
            return id;
        }

        if (underlyingType.IsEnum)
        {
            return Enum.Parse(underlyingType, id.ToString() ?? string.Empty, ignoreCase: true);
        }

        return Convert.ChangeType(id, underlyingType, System.Globalization.CultureInfo.InvariantCulture);
    }
}
