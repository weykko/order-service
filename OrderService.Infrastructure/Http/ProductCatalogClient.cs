using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OrderService.Application.Abstractions;
using OrderService.Application.DTOs;

namespace OrderService.Infrastructure.Http;

/// <summary>
/// HTTP-клиент системы продуктов (ProductService). Инкапсулирует REST-контракт
/// каталога: получение товара и синхронное резервирование склада.
/// </summary>
public class ProductCatalogClient : IProductCatalogClient
{
    private const string ProductByIdRoute = "api/v1/products/{0}";
    private const string ReserveStockRoute = "api/v1/products/{0}/reserve";

    private readonly HttpClient _httpClient;
    private readonly ILogger<ProductCatalogClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ProductCatalogClient(HttpClient httpClient, ILogger<ProductCatalogClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<ProductInfoDto?> GetProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var route = string.Format(ProductByIdRoute, productId);
        using var response = await _httpClient.GetAsync(route, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProductInfoDto>(_jsonOptions, cancellationToken);
    }

    public async Task<bool> ReserveStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default)
    {
        var route = string.Format(ReserveStockRoute, productId);
        using var response = await _httpClient.PostAsJsonAsync(route, new { quantity }, _jsonOptions, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Reserve request for product {ProductId} returned {StatusCode}", productId, response.StatusCode);
            return false;
        }

        // ProductService возвращает boolean-результат резервирования.
        var reserved = await response.Content.ReadFromJsonAsync<bool>(_jsonOptions, cancellationToken);
        return reserved;
    }
}
