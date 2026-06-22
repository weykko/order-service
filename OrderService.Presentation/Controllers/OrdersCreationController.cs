using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Abstractions;
using OrderService.Application.DTOs;

namespace OrderService.Presentation.Controllers;

/// <summary>
/// Оформление нового заказа.
/// </summary>
[ApiController]
[Route("api/v1/orders")]
public class OrdersCreationController(IOrderCreationService creationService) : ControllerBase
{
    private readonly IOrderCreationService _creationService = creationService;

    [HttpPost]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponseDto>> CreateOrder(
        [FromBody] CreateOrderDto dto,
        CancellationToken cancellationToken)
    {
        var order = await _creationService.CreateAsync(dto, cancellationToken);
        // Ссылка на GET-маршрут из OrdersQueryController по именованному роуту.
        return CreatedAtRoute(nameof(OrdersQueryController.GetOrder), new { id = order.Id }, order);
    }
}
