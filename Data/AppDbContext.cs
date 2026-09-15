using EfCoreWrapper.Models;
using Microsoft.EntityFrameworkCore;

namespace EfCoreWrapper.Data;

/// <summary>
/// Provides the EF Core database context used by the repository and unit of work abstractions.
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the customers set.
    /// </summary>
    public DbSet<Customer> Customers => Set<Customer>();

    /// <summary>
    /// Gets or sets the orders set.
    /// </summary>
    public DbSet<Order> Orders => Set<Order>();

    /// <summary>
    /// Gets or sets the order items set.
    /// </summary>
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(customer => customer.Id);
            entity.Property(customer => customer.Name)
                .HasMaxLength(200)
                .IsRequired();
            entity.Property(customer => customer.Email)
                .HasMaxLength(320)
                .IsRequired();
            entity.HasMany(customer => customer.Orders)
                .WithOne(order => order.Customer)
                .HasForeignKey(order => order.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(order => order.Id);
            entity.Property(order => order.OrderNumber)
                .HasMaxLength(64)
                .IsRequired();
            entity.Property(order => order.TotalAmount)
                .HasColumnType("decimal(18,2)");
            entity.HasMany(order => order.OrderItems)
                .WithOne(item => item.Order)
                .HasForeignKey(item => item.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.ProductName)
                .HasMaxLength(200)
                .IsRequired();
            entity.Property(item => item.UnitPrice)
                .HasColumnType("decimal(18,2)");
        });

        base.OnModelCreating(modelBuilder);
    }
}
