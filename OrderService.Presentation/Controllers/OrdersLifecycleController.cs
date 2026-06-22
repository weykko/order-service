using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Abstractions;
using OrderService.Application.DTOs;

namespace OrderService.Presentation.Controllers;

/// <summary>
/// Управление жизненным циклом заказа: оплата, сборка, доставка, получение,
/// возврат, отмена и произвольная смена статуса.
/// </summary>
[ApiController]
[Route("api/v1/orders")]
public class OrdersLifecycleController(IOrderLifecycleService lifecycleService) : ControllerBase
{
    private readonly IOrderLifecycleService _lifecycleService = lifecycleService;

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
