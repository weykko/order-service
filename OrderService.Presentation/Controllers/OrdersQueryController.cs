using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Abstractions;
using OrderService.Application.DTOs;

namespace OrderService.Presentation.Controllers;

/// <summary>
/// Чтение заказов: список с фильтрацией и пагинацией, заказ по id и история статусов.
/// </summary>
[ApiController]
[Route("api/v1/orders")]
public class OrdersQueryController(IOrderQueryService queryService) : ControllerBase
{
    private readonly IOrderQueryService _queryService = queryService;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OrderResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<OrderResponseDto>>> GetOrders(
        [FromQuery] OrderFilterDto filter,
        CancellationToken cancellationToken)
    {
        var result = await _queryService.GetFilteredAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}", Name = nameof(GetOrder))]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponseDto>> GetOrder(Guid id, CancellationToken cancellationToken)
    {
        var order = await _queryService.GetByIdAsync(id, cancellationToken);
        return Ok(order);
    }

    [HttpGet("{id:guid}/history")]
    [ProducesResponseType(typeof(IReadOnlyCollection<OrderStatusHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<OrderStatusHistoryDto>>> GetHistory(Guid id, CancellationToken cancellationToken)
    {
        var history = await _queryService.GetStatusHistoryAsync(id, cancellationToken);
        return Ok(history);
    }
}
