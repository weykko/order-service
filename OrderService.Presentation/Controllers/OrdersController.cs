using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Abstractions;
using OrderService.Application.DTOs;

namespace OrderService.Presentation.Controllers;

/// <summary>
/// REST API системы заказов: оформление, чтение и управление жизненным циклом.
/// Сценарии разнесены по сервисам (создание, чтение, переходы статусов) согласно SRP.
/// Валидация входных данных выполняется в прикладном слое (use-case).
/// </summary>
[ApiController]
[Route("api/v1/orders")]
public class OrdersController(
    IOrderCreationService creationService,
    IOrderQueryService queryService,
    IOrderLifecycleService lifecycleService) : ControllerBase
{
    private readonly IOrderCreationService _creationService = creationService;
    private readonly IOrderQueryService _queryService = queryService;
    private readonly IOrderLifecycleService _lifecycleService = lifecycleService;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OrderResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<OrderResponseDto>>> GetOrders(
        [FromQuery] OrderFilterDto filter,
        CancellationToken cancellationToken)
    {
        var result = await _queryService.GetFilteredAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
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

    [HttpPost]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponseDto>> CreateOrder(
        [FromBody] CreateOrderDto dto,
        CancellationToken cancellationToken)
    {
        var order = await _creationService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }

    [HttpPost("{id:guid}/pay")]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderResponseDto>> Pay(Guid id, CancellationToken cancellationToken)
    {
        var order = await _lifecycleService.PayAsync(id, cancellationToken);
        return Ok(order);
    }

    [HttpPost("{id:guid}/assemble")]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderResponseDto>> Assemble(Guid id, CancellationToken cancellationToken)
    {
        var order = await _lifecycleService.AssembleAsync(id, cancellationToken);
        return Ok(order);
    }

    [HttpPost("{id:guid}/ship")]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderResponseDto>> Ship(Guid id, CancellationToken cancellationToken)
    {
        var order = await _lifecycleService.ShipAsync(id, cancellationToken);
        return Ok(order);
    }

    [HttpPost("{id:guid}/deliver")]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderResponseDto>> Deliver(Guid id, CancellationToken cancellationToken)
    {
        var order = await _lifecycleService.DeliverAsync(id, cancellationToken);
        return Ok(order);
    }

    [HttpPost("{id:guid}/receive")]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderResponseDto>> Receive(Guid id, CancellationToken cancellationToken)
    {
        var order = await _lifecycleService.ReceiveAsync(id, cancellationToken);
        return Ok(order);
    }

    [HttpPost("{id:guid}/return")]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderResponseDto>> Return(
        Guid id,
        [FromBody] ReturnOrderRequest? request,
        CancellationToken cancellationToken)
    {
        var order = await _lifecycleService.ReturnAsync(id, request?.Reason, cancellationToken);
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
        var order = await _lifecycleService.CancelAsync(id, request?.Reason, cancellationToken);
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
        var order = await _lifecycleService.ChangeStatusAsync(id, dto, cancellationToken);
        return Ok(order);
    }
}

/// <summary>Тело запроса на отмену заказа.</summary>
public record CancelOrderRequest(string? Reason);

/// <summary>Тело запроса на возврат заказа.</summary>
public record ReturnOrderRequest(string? Reason);
