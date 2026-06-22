namespace OrderService.Application.DTOs;

/// <summary>
/// Параметры фильтрации и пагинации списка заказов.
/// </summary>
public class OrderFilterDto
{
    public string? Status { get; set; }
    public string? CustomerEmail { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
