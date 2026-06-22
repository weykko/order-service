namespace OrderService.Application.Events;

/// <summary>
/// Заказ оплачен. Контракт совместим с консьюмером системы продуктов:
/// топик "orderpaid", сериализация camelCase. ProductService по этому событию
/// списывает зарезервированный товар со склада (commit reservation).
/// </summary>
public class OrderPaidEvent
{
    public Guid OrderId { get; set; }
    public List<OrderPaidItem> Items { get; set; } = new();
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Позиция в событии оплаты заказа.
/// </summary>
public class OrderPaidItem
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal PriceAtPurchase { get; set; }
}
