using OrderService.Application.DTOs;

namespace OrderService.Application.Abstractions;

/// <summary>
/// Сценарий оформления нового заказа: проверка и резерв товара, расчёт суммы, сохранение.
/// </summary>
public interface IOrderCreationService
{
    Task<OrderResponseDto> CreateAsync(CreateOrderDto dto, CancellationToken cancellationToken = default);
}
