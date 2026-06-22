using OrderService.Domain.Common;
using OrderService.Domain.Enums;

namespace OrderService.Domain.Models;

/// <summary>
/// Запись истории смены статуса заказа. Хранится неизменяемой для аудита.
/// </summary>
public class OrderStatusHistory : BaseEntity
{
    public Guid OrderId { get; private set; }
    public OrderStatus? FromStatus { get; private set; }
    public OrderStatus ToStatus { get; private set; }
    public string? Comment { get; private set; }
    public DateTime ChangedAt { get; private set; }

    private OrderStatusHistory()
    {
    }

    public OrderStatusHistory(Guid orderId, OrderStatus? fromStatus, OrderStatus toStatus, string? comment = null)
    {
        OrderId = orderId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        Comment = comment;
        ChangedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Восстановление записи истории из хранилища (используется инфраструктурным слоем).
    /// </summary>
    public static OrderStatusHistory Rehydrate(
        Guid id,
        Guid orderId,
        OrderStatus? fromStatus,
        OrderStatus toStatus,
        string? comment,
        DateTime changedAt)
    {
        return new OrderStatusHistory(orderId, fromStatus, toStatus, comment)
        {
            Id = id,
            ChangedAt = changedAt
        };
    }
}
