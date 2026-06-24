using AutoMapper;
using Microsoft.Extensions.Logging;
using OrderService.Application.Abstractions;
using OrderService.Application.DTOs;
using OrderService.Application.Events;
using OrderService.Application.Exceptions;
using OrderService.Domain.Enums;
using OrderService.Domain.Interfaces;
using OrderService.Domain.Models;

namespace OrderService.Application.Services;

/// <summary>
/// Управление жизненным циклом заказа: доменные переходы статусов, сохранение
/// истории, инвалидация кеша, публикация событий и возврат денег там, где требуется.
/// </summary>
public class OrderLifecycleService : IOrderLifecycleService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderCache _cache;
    private readonly IEventPublisher _eventPublisher;
    private readonly IMapper _mapper;
    private readonly ILogger<OrderLifecycleService> _logger;

    public OrderLifecycleService(
        IOrderRepository orderRepository,
        IOrderCache cache,
        IEventPublisher eventPublisher,
        IMapper mapper,
        ILogger<OrderLifecycleService> logger)
    {
        _orderRepository = orderRepository;
        _cache = cache;
        _eventPublisher = eventPublisher;
        _mapper = mapper;
        _logger = logger;
    }

    public Task<OrderResponseDto> PayAsync(Guid id, CancellationToken cancellationToken = default) =>
        TransitionAsync(id, order => order.MarkAsPaid("Payment received (simulated)"), PublishPaidAsync, cancellationToken);

    public Task<OrderResponseDto> AssembleAsync(Guid id, CancellationToken cancellationToken = default) =>
        TransitionAsync(id, order => order.StartAssembling("Assembling started"), cancellationToken: cancellationToken);

    public Task<OrderResponseDto> ShipAsync(Guid id, CancellationToken cancellationToken = default) =>
        TransitionAsync(id, order => order.Ship("Shipped to delivery"), cancellationToken: cancellationToken);

    public Task<OrderResponseDto> DeliverAsync(Guid id, CancellationToken cancellationToken = default) =>
        TransitionAsync(id, order => order.Deliver("Delivered to pickup point"), cancellationToken: cancellationToken);

    public Task<OrderResponseDto> ReceiveAsync(Guid id, CancellationToken cancellationToken = default) =>
        TransitionAsync(id, order => order.Receive("Received by customer"), cancellationToken: cancellationToken);

    public Task<OrderResponseDto> ReturnAsync(Guid id, string? reason, CancellationToken cancellationToken = default) =>
        TransitionAsync(
            id,
            order => order.Return(reason ?? "Returned by customer"),
            (order, previous, ct) => RefundIfPaidAsync(order, previous, reason, ct),
            cancellationToken);

    public Task<OrderResponseDto> CancelAsync(Guid id, string? reason, CancellationToken cancellationToken = default) =>
        TransitionAsync(
            id,
            order => order.Cancel(reason ?? "Cancelled by request"),
            (order, _, ct) => PublishCancelledAsync(order, reason, ct),
            cancellationToken);

    public Task<OrderResponseDto> ChangeStatusAsync(Guid id, ChangeStatusDto dto, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<OrderStatus>(dto.Status, true, out var targetStatus))
            throw new BusinessRuleException($"Unknown order status '{dto.Status}'");

        return TransitionAsync(id, order => order.ChangeStatus(targetStatus, dto.Comment), cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Единый шаблон перехода: загрузка заказа, доменный переход, сохранение,
    /// публикация события смены статуса и опциональное побочное действие (событие оплаты/возврата).
    /// </summary>
    private async Task<OrderResponseDto> TransitionAsync(
        Guid id,
        Action<Order> transition,
        Func<Order, OrderStatus, CancellationToken, Task>? afterTransition = null,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetRequiredAsync(id, cancellationToken);
        var previous = order.Status;

        transition(order);

        var lastHistory = order.StatusHistory.Last();
        await _orderRepository.UpdateStatusAsync(order, lastHistory, cancellationToken);
        await _cache.RemoveAsync(order.Id, cancellationToken);

        await PublishStatusChangedAsync(order, previous, order.Status, cancellationToken);

        if (afterTransition != null)
            await afterTransition(order, previous, cancellationToken);

        _logger.LogInformation("Order {OrderId} status changed from {From} to {To}", order.Id, previous, order.Status);
        return _mapper.Map<OrderResponseDto>(order);
    }

    /// <summary>
    /// Публикует событие отмены для возврата зарезервированного товара на склад
    /// (его слушает ProductService). Возврат денег не требуется: отмена возможна
    /// только до оплаты (из статуса Created).
    /// </summary>
    private Task PublishCancelledAsync(Order order, string? reason, CancellationToken cancellationToken) =>
        _eventPublisher.PublishAsync(new OrderCancelledEvent
        {
            OrderId = order.Id,
            Reason = reason,
            CancelledAt = DateTime.UtcNow,
            Items = order.Items.Select(i => new OrderCancelledItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity
            }).ToList()
        }, cancellationToken);

    private Task PublishPaidAsync(Order order, OrderStatus previous, CancellationToken cancellationToken) =>
        _eventPublisher.PublishAsync(new OrderPaidEvent
        {
            OrderId = order.Id,
            PaidAt = DateTime.UtcNow,
            Items = order.Items.Select(i => new OrderPaidItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                PriceAtPurchase = i.UnitPrice.Amount
            }).ToList()
        }, cancellationToken);

    /// <summary>
    /// Публикует событие возврата денег, если заказ до перехода был в оплаченном состоянии.
    /// Используется при возврате доставленного заказа (Returned).
    /// </summary>
    private async Task RefundIfPaidAsync(Order order, OrderStatus previousStatus, string? reason, CancellationToken cancellationToken)
    {
        if (!Order.IsPaidStatus(previousStatus))
            return;

        await _eventPublisher.PublishAsync(new OrderRefundedEvent
        {
            OrderId = order.Id,
            Amount = order.TotalAmount.Amount,
            Currency = order.Currency,
            Reason = reason,
            RefundedAt = DateTime.UtcNow
        }, cancellationToken);

        _logger.LogInformation("Order {OrderId} refunded: {Amount} {Currency}",
            order.Id, order.TotalAmount.Amount, order.Currency);
    }

    private Task PublishStatusChangedAsync(Order order, OrderStatus from, OrderStatus to, CancellationToken cancellationToken) =>
        _eventPublisher.PublishAsync(new OrderStatusChangedEvent
        {
            OrderId = order.Id,
            FromStatus = from.ToString(),
            ToStatus = to.ToString(),
            ChangedAt = DateTime.UtcNow
        }, cancellationToken);
}
