namespace OrderService.Infrastructure.Http;

/// <summary>
/// Настройки HTTP-клиента системы продуктов.
/// </summary>
public class ProductCatalogOptions
{
    public const string SectionName = "ProductCatalog";

    public string BaseUrl { get; set; } = "http://localhost:8080";
    public int TimeoutSeconds { get; set; } = 10;
}
