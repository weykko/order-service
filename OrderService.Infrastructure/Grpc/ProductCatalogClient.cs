using Grpc.Core;
using Microsoft.Extensions.Logging;
using OrderService.Application.Abstractions;
using OrderService.Application.DTOs;
using ProductServiceGrpc.Grpc;
using StockServiceGrpc.Grpc;

namespace OrderService.Infrastructure.Grpc;

/// <summary>
/// gRPC-клиент системы продуктов (ProductService). Инкапсулирует синхронные
/// межсервисные вызовы: получение товара (сервис ProductServiceGrpc) и
/// резервирование склада (сервис StockServiceGrpc).
/// </summary>
public class ProductCatalogClient : IProductCatalogClient
{
    private readonly ProductServiceGrpc.Grpc.ProductServiceGrpc.ProductServiceGrpcClient _productClient;
    private readonly StockServiceGrpc.Grpc.StockServiceGrpc.StockServiceGrpcClient _stockClient;
    private readonly ILogger<ProductCatalogClient> _logger;

    public ProductCatalogClient(
        ProductServiceGrpc.Grpc.ProductServiceGrpc.ProductServiceGrpcClient productClient,
        StockServiceGrpc.Grpc.StockServiceGrpc.StockServiceGrpcClient stockClient,
        ILogger<ProductCatalogClient> logger)
    {
        _productClient = productClient;
        _stockClient = stockClient;
        _logger = logger;
    }

    public async Task<ProductInfoDto?> GetProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        try
        {
            var reply = await _productClient.GetProductAsync(
                new GetProductRequest { Id = productId.ToString() },
                cancellationToken: cancellationToken);

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
        catch (RpcException ex) when (IsProductNotFound(ex))
        {
            // Товар не найден в системе продуктов: возвращаем null,
            // прикладной слой превратит это в корректный 404, а не 500.
            _logger.LogInformation("Product {ProductId} not found in catalog: {Status}", productId, ex.StatusCode);
            return null;
        }
    }

    /// <summary>
    /// Определяет, означает ли gRPC-ошибка отсутствие товара. Помимо штатного
    /// NotFound учитываем Unknown/Internal: система продуктов пробрасывает
    /// доменное "не найдено" необёрнутым, и gRPC мапит его в Unknown.
    /// </summary>
    private static bool IsProductNotFound(RpcException ex)
    {
        if (ex.StatusCode == StatusCode.NotFound)
            return true;

        if (ex.StatusCode is StatusCode.Unknown or StatusCode.Internal)
            return ex.Status.Detail.Contains("not found", StringComparison.OrdinalIgnoreCase);

        return false;
    }

    public async Task<bool> ReserveStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
    {
        try
        {
            var reply = await _stockClient.ReserveStockAsync(
                new ReserveStockRequest { ProductId = productId.ToString(), Quantity = quantity },
                cancellationToken: cancellationToken);

            if (!reply.Success)
                _logger.LogWarning("Reserve for product {ProductId} declined: {Message}", productId, reply.Message);

            return reply.Success;
        }
        catch (RpcException ex)
        {
            _logger.LogWarning(ex, "gRPC ReserveStock for product {ProductId} failed: {Status}", productId, ex.StatusCode);
            return false;
        }
    }
}
