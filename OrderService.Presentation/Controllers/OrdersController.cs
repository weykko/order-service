using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Abstractions;
using OrderService.Application.DTOs;

namespace OrderService.Presentation.Controllers;

/// <summary>
/// REST API системы заказов: оформление, чтение, оплата, отмена и смена статусов.
/// Валидация входных данных выполняется в прикладном слое (use-case).
/// </summary>
[ApiController]
[Route("api/v1/orders")]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    private readonly IOrderService _orderService = orderService;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OrderResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<OrderResponseDto>>> GetOrders(
        [FromQuery] OrderFilterDto filter,
        CancellationToken cancellationToken)
    {
        var result = await _orderService.GetFilteredAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponseDto>> GetOrder(Guid id, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetByIdAsync(id, cancellationToken);
        return Ok(order);
    }

    [HttpGet("{id:guid}/history")]
    [ProducesResponseType(typeof(IReadOnlyCollection<OrderStatusHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<OrderStatusHistoryDto>>> GetHistory(Guid id, CancellationToken cancellationToken)
    {
        var history = await _orderService.GetStatusHistoryAsync(id, cancellationToken);
        return Ok(history);
    }

    [HttpPost]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponseDto>> CreateOrder(
        [FromBody] CreateOrderDto dto,
        CancellationToken cancellationToken)
    {
        var order = await _orderService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }

    [HttpPost("{id:guid}/pay")]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderResponseDto>> Pay(Guid id, CancellationToken cancellationToken)
    {
        var order = await _orderService.PayAsync(id, cancellationToken);
        return Ok(order);
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderResponseDto>> Cancel(
        Guid id,
        [FromBody] CancelOrderRequest? request,
        CancellationToken cancellationToken)
    {
        var order = await _orderService.CancelAsync(id, request?.Reason, cancellationToken);
        return Ok(order);
    }

    [HttpPost("{id:guid}/status")]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderResponseDto>> ChangeStatus(
        Guid id,
        [FromBody] ChangeStatusDto dto,
        CancellationToken cancellationToken)
    {
        var order = await _orderService.ChangeStatusAsync(id, dto, cancellationToken);
        return Ok(order);
    }
}

/// <summary>
/// Тело запроса на отмену заказа.
/// </summary>
public record CancelOrderRequest(string? Reason);
