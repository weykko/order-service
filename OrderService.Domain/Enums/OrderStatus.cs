namespace OrderService.Domain.Enums;

/// <summary>
/// Жизненный цикл заказа.
/// Created -> Paid -> Assembling -> Shipped -> Delivered,
/// либо отмена (Cancelled) до момента отгрузки.
/// </summary>
public enum OrderStatus
{
    Created = 1,
    Paid = 2,
    Assembling = 3,
    Shipped = 4,
    Delivered = 5,
    Cancelled = 6
}
