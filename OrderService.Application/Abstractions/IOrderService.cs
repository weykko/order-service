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

    /// <summary>Отменяет заказ и публикует событие отмены.</summary>
    Task<OrderResponseDto> CancelAsync(Guid id, string? reason, CancellationToken cancellationToken = default);

    /// <summary>Произвольный переход статуса через стейт-машину заказа.</summary>
    Task<OrderResponseDto> ChangeStatusAsync(Guid id, ChangeStatusDto dto, CancellationToken cancellationToken = default);
}
