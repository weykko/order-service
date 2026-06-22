using OrderService.Domain.Enums;
using OrderService.Domain.Models;

namespace OrderService.Domain.Interfaces;

/// <summary>
/// Параметры фильтрации и постраничного чтения списка заказов.
/// </summary>
public sealed record OrderQuery(
    OrderStatus? Status,
    string? CustomerEmail,
    int Page,
    int PageSize)
{
    public int Offset => (Page - 1) * PageSize;
}

/// <summary>
/// Хранилище заказов. Реализация должна сохранять заказ вместе с позициями
/// и историей статусов атомарно.
/// </summary>
public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken cancellationToken = default);

    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Order>> GetListAsync(OrderQuery query, CancellationToken cancellationToken = default);

    Task<int> CountAsync(OrderQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<OrderStatusHistory>> GetStatusHistoryAsync(Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновляет статус заказа и добавляет новую запись в историю в одной транзакции.
    /// </summary>
    Task UpdateStatusAsync(Order order, OrderStatusHistory newHistoryEntry, CancellationToken cancellationToken = default);
}
