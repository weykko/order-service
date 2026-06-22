using OrderService.Application.Exceptions;
using OrderService.Domain.Interfaces;
using OrderService.Domain.Models;

namespace OrderService.Application.Services;

/// <summary>
/// Вспомогательные методы доступа к заказам, переиспользуемые сценариями приложения.
/// </summary>
internal static class OrderRepositoryExtensions
{
    /// <summary>Загружает заказ по идентификатору или бросает <see cref="NotFoundException"/>.</summary>
    public static async Task<Order> GetRequiredAsync(
        this IOrderRepository repository, Guid id, CancellationToken cancellationToken)
        => await repository.GetByIdAsync(id, cancellationToken)
           ?? throw new NotFoundException("Order", id);
}
