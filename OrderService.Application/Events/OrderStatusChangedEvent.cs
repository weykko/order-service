namespace OrderService.Application.Events;

/// <summary>
/// Статус заказа изменился. Публикуется при любом переходе стейт-машины.
/// </summary>
public class OrderStatusChangedEvent
{
    public Guid OrderId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
