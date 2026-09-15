namespace EfCoreWrapper.Models;

/// <summary>
/// Represents a sample order item entity.
/// </summary>
public class OrderItem
{
    /// <summary>
    /// Gets or sets the order item identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the order identifier.
    /// </summary>
    public int OrderId { get; set; }

    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quantity.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the unit price.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Gets or sets the owning order.
    /// </summary>
    public Order? Order { get; set; }
}
