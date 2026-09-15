# efcore-wrapper

A comprehensive Entity Framework Core 8.0 wrapper with complete CRUD operations, Unit of Work pattern, transaction handling, eager loading support, and raw SQL execution.

## Features

- Generic `IRepository<T>` abstraction for any entity type
- Async CRUD methods for single-entity and bulk operations
- Filtering, sorting, pagination, and eager-loading support
- Raw SQL command execution through EF Core
- `IUnitOfWork` for coordinating multiple repositories
- Transaction support with begin, commit, and rollback methods
- Logging via `ILogger`
- XML documentation and defensive argument validation

## Project structure

```text
Data/
  AppDbContext.cs
  Exceptions/
    RepositoryException.cs
  Repository/
    IRepository.cs
    Repository.cs
  UnitOfWork/
    IUnitOfWork.cs
    UnitOfWork.cs
Models/
  Customer.cs
  Order.cs
  OrderItem.cs
EfCoreWrapper.csproj
```

## Example registration

```csharp
using EfCoreWrapper.Data;
using EfCoreWrapper.Data.UnitOfWork;
using Microsoft.EntityFrameworkCore;

var services = new ServiceCollection();

services.AddLogging();
services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=EfCoreWrapper;Trusted_Connection=True;"));
services.AddScoped<IUnitOfWork, UnitOfWork>();
```

## Example usage

```csharp
using EfCoreWrapper.Data.UnitOfWork;
using EfCoreWrapper.Models;
using Microsoft.EntityFrameworkCore;

public sealed class CustomerService
{
    private readonly IUnitOfWork _unitOfWork;

    public CustomerService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<Customer>> GetActiveCustomersAsync(CancellationToken cancellationToken)
    {
        return await _unitOfWork.Repository<Customer>().GetAllAsync(
            predicate: customer => customer.IsActive,
            orderBy: query => query.OrderBy(customer => customer.Name),
            pageNumber: 1,
            pageSize: 25,
            include: query => query
                .Include(customer => customer.Orders)
                .ThenInclude(order => order.OrderItems),
            cancellationToken: cancellationToken);
    }

    public async Task<int> CreateCustomerAsync(Customer customer, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            await _unitOfWork.Repository<Customer>().CreateAsync(customer, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return customer.Id;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public Task<int> ArchiveInactiveCustomersAsync(DateTime cutoffDate, CancellationToken cancellationToken)
    {
        const string sql = """
                           UPDATE Customers
                           SET IsActive = 0
                           WHERE CreatedUtc < {0} AND IsActive = 1
                           """;

        return _unitOfWork.Repository<Customer>().ExecuteSqlAsync(sql, cutoffDate);
    }
}
```

## Build

```bash
dotnet build
```
