using OrderService.Application.DTOs;

namespace OrderService.Application.Abstractions;

/// <summary>
/// Кеш чтения заказов (реализуется через Redis + in-memory).
/// </summary>
public interface IOrderCache
{
    Task<OrderResponseDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task SetAsync(Guid id, OrderResponseDto order, TimeSpan ttl, CancellationToken cancellationToken = default);

    Task RemoveAsync(Guid id, CancellationToken cancellationToken = default);
}
