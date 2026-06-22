namespace OrderService.Application.DTOs;

/// <summary>
/// Запрос на ручную смену статуса заказа.
/// </summary>
public class ChangeStatusDto
{
    public string Status { get; set; } = string.Empty;
    public string? Comment { get; set; }
}
