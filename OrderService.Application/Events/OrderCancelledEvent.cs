namespace OrderService.Application.Events;

/// <summary>
/// Заказ отменён. Публикуется при отмене заказа для возврата зарезервированного
/// товара на склад (топик "ordercancelled"). ProductService слушает это событие
/// и вызывает отмену резервирования по каждой позиции.
/// </summary>
public class OrderCancelledEvent
{
    public Guid OrderId { get; set; }
    public List<OrderCancelledItem> Items { get; set; } = new();
    public string? Reason { get; set; }
    public DateTime CancelledAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Позиция в событии отмены заказа.
/// </summary>
public class OrderCancelledItem
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
