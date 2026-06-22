namespace OrderService.Application.DTOs;

/// <summary>
/// Запрос на оформление нового заказа.
/// </summary>
public class CreateOrderDto
{
    public CustomerInfoDto Customer { get; set; } = new();
    public List<CreateOrderItemDto> Items { get; set; } = new();
    public string Currency { get; set; } = "RUB";
}

/// <summary>
/// Позиция в запросе на оформление заказа.
/// Цена не передаётся клиентом — она берётся из системы продуктов.
/// </summary>
public class CreateOrderItemDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
