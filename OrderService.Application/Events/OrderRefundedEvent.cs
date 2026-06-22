namespace OrderService.Application.Events;

/// <summary>
/// По заказу произведён возврат денежных средств. Публикуется при отмене
/// оплаченного заказа или при возврате доставленного заказа (топик "orderrefunded").
/// </summary>
public class OrderRefundedEvent
{
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime RefundedAt { get; set; } = DateTime.UtcNow;
}
