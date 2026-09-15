using EfCoreWrapper.Data.Exceptions;
using EfCoreWrapper.Data.Internal;
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
        return await DbContextOperations.SaveChangesAsync(_context, _logger, "unit of work", cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return await DbContextOperations.BeginTransactionAsync(_context, _logger, "unit of work", cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await DbContextOperations.CommitTransactionAsync(_context, _logger, "unit of work", cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await DbContextOperations.RollbackTransactionAsync(_context, _logger, "unit of work", cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _repositories.Clear();
        _disposed = true;
        await ValueTask.CompletedTask.ConfigureAwait(false);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
