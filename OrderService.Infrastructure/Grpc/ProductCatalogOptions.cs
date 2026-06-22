namespace OrderService.Infrastructure.Grpc;

/// <summary>
/// Настройки gRPC-клиента системы продуктов (ProductService).
/// </summary>
public class ProductCatalogOptions
{
    public const string SectionName = "ProductCatalog";

    /// <summary>Адрес gRPC-эндпоинта ProductService (например, http://productservice:8080).</summary>
    public string GrpcAddress { get; set; } = "http://localhost:8080";
}
