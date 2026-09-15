using EfCoreWrapper.Data.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace EfCoreWrapper.Data.Internal;

internal static class DbContextOperations
{
    public static async Task<int> SaveChangesAsync(
        DbContext context,
        ILogger logger,
        string sourceName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to save changes in {SourceName}.", sourceName);
            throw new RepositoryException($"Failed to save changes in {sourceName}.", exception);
        }
    }

    public static async Task<IDbContextTransaction> BeginTransactionAsync(
        DbContext context,
        ILogger logger,
        string sourceName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to begin a transaction in {SourceName}.", sourceName);
            throw new RepositoryException($"Failed to begin a transaction in {sourceName}.", exception);
        }
    }

    public static async Task CommitTransactionAsync(
        DbContext context,
        ILogger logger,
        string sourceName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var transaction = context.Database.CurrentTransaction
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
            logger.LogError(exception, "Failed to commit the current transaction in {SourceName}.", sourceName);
            throw new RepositoryException($"Failed to commit the current transaction in {sourceName}.", exception);
        }
    }

    public static async Task RollbackTransactionAsync(
        DbContext context,
        ILogger logger,
        string sourceName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var transaction = context.Database.CurrentTransaction
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
            logger.LogError(exception, "Failed to roll back the current transaction in {SourceName}.", sourceName);
            throw new RepositoryException($"Failed to roll back the current transaction in {sourceName}.", exception);
        }
    }
}
