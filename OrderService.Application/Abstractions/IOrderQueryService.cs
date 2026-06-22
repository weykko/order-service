using OrderService.Application.DTOs;

namespace OrderService.Application.Abstractions;

/// <summary>
/// Сценарии чтения заказов: по идентификатору (с кешем), постранично с фильтрацией,
/// а также история смены статусов.
/// </summary>
public interface IOrderQueryService
{
    Task<OrderResponseDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<OrderResponseDto>> GetFilteredAsync(OrderFilterDto filter, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<OrderStatusHistoryDto>> GetStatusHistoryAsync(Guid id, CancellationToken cancellationToken = default);
}
