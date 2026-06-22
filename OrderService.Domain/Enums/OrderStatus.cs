namespace OrderService.Domain.Enums;

/// <summary>
/// Жизненный цикл заказа.
/// Created -> Paid -> Assembling -> Shipped -> Delivered -> Received | Returned.
/// Отмена (Cancelled) возможна только до отправки в сборку (Created, Paid).
/// Финальные статусы: Received, Returned, Cancelled.
/// </summary>
public enum OrderStatus
{
    Created = 1,
    Paid = 2,
    Assembling = 3,
    Shipped = 4,
    Delivered = 5,
    Cancelled = 6,
    Received = 7,
    Returned = 8
}
