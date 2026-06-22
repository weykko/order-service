using Grpc.Core;
using Microsoft.Extensions.Logging;
using OrderService.Application.Abstractions;
using OrderService.Application.DTOs;
using ProductCatalog.Grpc;

namespace OrderService.Infrastructure.Grpc;

/// <summary>
/// gRPC-клиент системы продуктов (ProductService). Инкапсулирует синхронные
/// межсервисные вызовы: получение товара и резервирование склада.
/// </summary>
public class ProductCatalogClient : IProductCatalogClient
{
    private readonly ProductCatalog.Grpc.ProductCatalog.ProductCatalogClient _client;
    private readonly ILogger<ProductCatalogClient> _logger;

    public ProductCatalogClient(
        ProductCatalog.Grpc.ProductCatalog.ProductCatalogClient client,
        ILogger<ProductCatalogClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<ProductInfoDto?> GetProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var reply = await _client.GetProductAsync(
            new GetProductRequest { ProductId = productId.ToString() },
            cancellationToken: cancellationToken);

        if (!reply.Found)
            return null;

        return new ProductInfoDto
        {
            Id = Guid.Parse(reply.Id),
            Name = reply.Name,
            Price = (decimal)reply.Price,
            Currency = reply.Currency,
            AvailableStock = reply.AvailableStock,
            IsInStock = reply.IsInStock
        };
    }

    public async Task<bool> ReserveStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
    {
        try
        {
            var reply = await _client.ReserveStockAsync(
                new ReserveStockRequest { ProductId = productId.ToString(), Quantity = quantity },
                cancellationToken: cancellationToken);

            return reply.Reserved;
        }
        catch (RpcException ex)
        {
            _logger.LogWarning(ex, "gRPC ReserveStock for product {ProductId} failed: {Status}", productId, ex.StatusCode);
            return false;
        }
    }
}
