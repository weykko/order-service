namespace OrderService.Presentation.Controllers;

/// <summary>Тело запроса на отмену заказа.</summary>
public record CancelOrderRequest(string? Reason);

/// <summary>Тело запроса на возврат заказа.</summary>
public record ReturnOrderRequest(string? Reason);
