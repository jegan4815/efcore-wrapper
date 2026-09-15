using EfCoreWrapper.Data.Exceptions;
using EfCoreWrapper.Data.Repository;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EfCoreWrapper.Data.UnitOfWork;

/// <summary>
/// Manages repositories and transactions over a shared database context.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UnitOfWork> _logger;
    private readonly Dictionary<Type, object> _repositories = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWork"/> class.
    /// </summary>
    /// <param name="context">The shared database context.</param>
    /// <param name="serviceProvider">The service provider used to resolve repository dependencies.</param>
    /// <param name="logger">The logger.</param>
    public UnitOfWork(AppDbContext context, IServiceProvider serviceProvider, ILogger<UnitOfWork> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IRepository<T> Repository<T>()
        where T : class
    {
        ThrowIfDisposed();

        try
        {
            if (_repositories.TryGetValue(typeof(T), out var repository))
            {
                return (IRepository<T>)repository;
            }

            var createdRepository = ActivatorUtilities.CreateInstance<Repository<T>>(_serviceProvider, _context);
            _repositories[typeof(T)] = createdRepository;
            return createdRepository;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to resolve repository for entity type {EntityType}.", typeof(T).Name);
            throw new RepositoryException($"Failed to resolve repository for entity type {typeof(T).Name}.", exception);
        }
    }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            return await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to save changes in the unit of work.");
            throw new RepositoryException("Failed to save changes in the unit of work.", exception);
        }
    }

    /// <inheritdoc />
    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            return await _context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to begin a transaction.");
            throw new RepositoryException("Failed to begin a transaction.", exception);
        }
    }

    /// <inheritdoc />
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

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
            _logger.LogError(exception, "Failed to commit the current transaction.");
            throw new RepositoryException("Failed to commit the current transaction.", exception);
        }
    }

    /// <inheritdoc />
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

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
            _logger.LogError(exception, "Failed to roll back the current transaction.");
            throw new RepositoryException("Failed to roll back the current transaction.", exception);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _repositories.Clear();
        await _context.DisposeAsync().ConfigureAwait(false);
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
