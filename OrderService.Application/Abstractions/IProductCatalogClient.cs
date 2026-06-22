using OrderService.Application.DTOs;

namespace OrderService.Application.Abstractions;

/// <summary>
/// Клиент системы продуктов (ProductService). Используется для получения
/// актуальной информации о товаре и синхронного резервирования склада по HTTP.
/// </summary>
public interface IProductCatalogClient
{
    /// <summary>Возвращает сведения о товаре или null, если товар не найден.</summary>
    Task<ProductInfoDto?> GetProductAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>Пытается зарезервировать указанное количество товара на складе.</summary>
    Task<bool> ReserveStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);
}
