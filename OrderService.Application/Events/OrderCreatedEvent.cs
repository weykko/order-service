namespace OrderService.Application.Events;

/// <summary>
/// Заказ создан. Публикуется для аналитики/нотификаций.
/// </summary>
public class OrderCreatedEvent
{
    public Guid OrderId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
