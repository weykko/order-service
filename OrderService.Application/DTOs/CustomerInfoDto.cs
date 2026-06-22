namespace OrderService.Application.DTOs;

/// <summary>
/// Контактные данные покупателя для оформления заказа.
/// </summary>
public class CustomerInfoDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
}
