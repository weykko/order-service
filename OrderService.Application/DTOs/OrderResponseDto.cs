namespace OrderService.Application.DTOs;

/// <summary>
/// Представление заказа для клиента.
/// </summary>
public class OrderResponseDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public CustomerInfoDto Customer { get; set; } = new();
    public List<OrderItemDto> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Позиция заказа в ответе клиенту.
/// </summary>
public class OrderItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
}
