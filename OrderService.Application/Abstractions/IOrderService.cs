using OrderService.Application.DTOs;

namespace OrderService.Application.Abstractions;

/// <summary>
/// Сценарии управления заказами (use cases).
/// </summary>
public interface IOrderService
{
    Task<OrderResponseDto> CreateAsync(CreateOrderDto dto, CancellationToken cancellationToken = default);

    Task<OrderResponseDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<OrderResponseDto>> GetFilteredAsync(OrderFilterDto filter, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<OrderStatusHistoryDto>> GetStatusHistoryAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Симулированная оплата заказа: переводит в статус Paid и публикует событие оплаты.</summary>
    Task<OrderResponseDto> PayAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Переводит оплаченный заказ в сборку (Assembling).</summary>
    Task<OrderResponseDto> AssembleAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Передаёт собранный заказ в доставку (Shipped).</summary>
    Task<OrderResponseDto> ShipAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Помечает заказ доставленным (Delivered).</summary>
    Task<OrderResponseDto> DeliverAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Отменяет заказ и публикует событие отмены.</summary>
    Task<OrderResponseDto> CancelAsync(Guid id, string? reason, CancellationToken cancellationToken = default);

    /// <summary>Произвольный переход статуса через стейт-машину заказа.</summary>
    Task<OrderResponseDto> ChangeStatusAsync(Guid id, ChangeStatusDto dto, CancellationToken cancellationToken = default);
}
