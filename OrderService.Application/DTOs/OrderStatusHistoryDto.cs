namespace OrderService.Application.DTOs;

/// <summary>
/// Запись истории смены статуса заказа.
/// </summary>
public class OrderStatusHistoryDto
{
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public DateTime ChangedAt { get; set; }
}
