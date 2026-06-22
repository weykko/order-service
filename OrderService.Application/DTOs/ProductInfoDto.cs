namespace OrderService.Application.DTOs;

/// <summary>
/// Сведения о товаре, получаемые из системы продуктов (ProductService).
/// Содержит подмножество полей ProductResponseDto, необходимое системе заказов.
/// </summary>
public class ProductInfoDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "RUB";
    public int AvailableStock { get; set; }
    public bool IsInStock { get; set; }
}
