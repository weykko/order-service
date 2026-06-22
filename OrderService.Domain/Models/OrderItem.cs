using OrderService.Domain.Common;
using OrderService.Domain.Exceptions;
using OrderService.Domain.ValueObjects;

namespace OrderService.Domain.Models;

/// <summary>
/// Позиция заказа: товар, его количество и цена, зафиксированная в момент покупки.
/// </summary>
public class OrderItem : BaseEntity
{
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; }
    public Money UnitPrice { get; private set; }
    public int Quantity { get; private set; }

    public Money LineTotal => UnitPrice.Multiply(Quantity);

    private OrderItem()
    {
        ProductName = string.Empty;
        UnitPrice = Money.Zero();
    }

    public OrderItem(Guid productId, string productName, Money unitPrice, int quantity)
    {
        if (productId == Guid.Empty)
            throw new DomainException("ProductId is required for an order item");

        if (string.IsNullOrWhiteSpace(productName))
            throw new DomainException("Product name is required for an order item");

        if (quantity <= 0)
            throw new DomainException("Order item quantity must be positive");

        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice ?? throw new DomainException("Unit price is required for an order item");
        Quantity = quantity;
    }

    internal void AttachToOrder(Guid orderId) => OrderId = orderId;

    /// <summary>
    /// Восстановление позиции из хранилища (используется инфраструктурным слоем).
    /// </summary>
    public static OrderItem Rehydrate(
        Guid id,
        Guid orderId,
        Guid productId,
        string productName,
        Money unitPrice,
        int quantity,
        DateTime createdAt)
    {
        var item = new OrderItem(productId, productName, unitPrice, quantity)
        {
            Id = id,
            OrderId = orderId,
            CreatedAt = createdAt
        };
        return item;
    }
}
