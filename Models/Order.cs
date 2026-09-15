namespace EfCoreWrapper.Models;

/// <summary>
/// Represents a sample order entity.
/// </summary>
public class Order
{
    /// <summary>
    /// Gets or sets the order identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the customer identifier.
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the order number.
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the order total amount.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the order was created.
    /// </summary>
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the owning customer.
    /// </summary>
    public Customer? Customer { get; set; }

    /// <summary>
    /// Gets the order items.
    /// </summary>
    public ICollection<OrderItem> OrderItems { get; } = new List<OrderItem>();
}
