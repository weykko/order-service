using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using OrderService.Application.Abstractions;
using OrderService.Application.DTOs;
using OrderService.Application.Events;
using OrderService.Application.Exceptions;
using OrderService.Domain.Enums;
using OrderService.Domain.Interfaces;
using OrderService.Domain.Models;
using OrderService.Domain.ValueObjects;

namespace OrderService.Application.Services;

/// <summary>
/// Реализация сценариев управления заказами: оформление с проверкой и резервом
/// товара в системе продуктов, оплата, отмена и переходы статусов с сохранением истории.
/// </summary>
public class OrderManagementService : IOrderService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly IOrderRepository _orderRepository;
    private readonly IProductCatalogClient _catalogClient;
    private readonly IOrderCache _cache;
    private readonly IEventPublisher _eventPublisher;
    private readonly IValidator<CreateOrderDto> _createOrderValidator;
    private readonly IValidator<OrderFilterDto> _orderFilterValidator;
    private readonly IMapper _mapper;
    private readonly ILogger<OrderManagementService> _logger;

    public OrderManagementService(
        IOrderRepository orderRepository,
        IProductCatalogClient catalogClient,
        IOrderCache cache,
        IEventPublisher eventPublisher,
        IValidator<CreateOrderDto> createOrderValidator,
        IValidator<OrderFilterDto> orderFilterValidator,
        IMapper mapper,
        ILogger<OrderManagementService> logger)
    {
        _orderRepository = orderRepository;
        _catalogClient = catalogClient;
        _cache = cache;
        _eventPublisher = eventPublisher;
        _createOrderValidator = createOrderValidator;
        _orderFilterValidator = orderFilterValidator;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<OrderResponseDto> CreateAsync(CreateOrderDto dto, CancellationToken cancellationToken = default)
    {
        await _createOrderValidator.ValidateAndThrowAsync(dto, cancellationToken);

        var customer = new CustomerInfo(dto.Customer.FullName, dto.Customer.Email, dto.Customer.Phone, dto.Customer.ShippingAddress);
        var currency = dto.Currency.ToUpperInvariant();

        var orderItems = new List<OrderItem>();
        foreach (var line in dto.Items)
        {
            var product = await _catalogClient.GetProductAsync(line.ProductId, cancellationToken)
                          ?? throw new NotFoundException("Product", line.ProductId);

            if (!await _catalogClient.ReserveStockAsync(line.ProductId, line.Quantity, cancellationToken))
                throw new BusinessRuleException(
                    $"Insufficient stock for product '{product.Name}' ({line.ProductId}). Requested: {line.Quantity}");

            var unitPrice = new Money(product.Price, product.Currency);
            orderItems.Add(new OrderItem(product.Id, product.Name, unitPrice, line.Quantity));
        }

        var order = new Order(customer, orderItems, currency);
        await _orderRepository.AddAsync(order, cancellationToken);

        _logger.LogInformation("Order {OrderId} created with {ItemCount} items, total {Total} {Currency}",
            order.Id, orderItems.Count, order.TotalAmount.Amount, order.Currency);

        await _eventPublisher.PublishAsync(new OrderCreatedEvent
        {
            OrderId = order.Id,
            CustomerEmail = customer.Email,
            TotalAmount = order.TotalAmount.Amount,
            Currency = order.Currency,
            CreatedAt = order.CreatedAt
        }, cancellationToken);

        return _mapper.Map<OrderResponseDto>(order);
    }

    public async Task<OrderResponseDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cached = await _cache.GetAsync(id, cancellationToken);
        if (cached != null)
            return cached;

        var order = await LoadOrderAsync(id, cancellationToken);
        var dto = _mapper.Map<OrderResponseDto>(order);

        await _cache.SetAsync(id, dto, CacheTtl, cancellationToken);
        return dto;
    }

    public async Task<PagedResult<OrderResponseDto>> GetFilteredAsync(OrderFilterDto filter, CancellationToken cancellationToken = default)
    {
        await _orderFilterValidator.ValidateAndThrowAsync(filter, cancellationToken);

        OrderStatus? status = string.IsNullOrWhiteSpace(filter.Status)
            ? null
            : Enum.Parse<OrderStatus>(filter.Status, true);

        var query = new OrderQuery(status, filter.CustomerEmail, filter.Page, filter.PageSize);

        var orders = await _orderRepository.GetListAsync(query, cancellationToken);
        var totalCount = await _orderRepository.CountAsync(query, cancellationToken);

        return new PagedResult<OrderResponseDto>
        {
            Items = orders.Select(_mapper.Map<OrderResponseDto>).ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<IReadOnlyCollection<OrderStatusHistoryDto>> GetStatusHistoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (await _orderRepository.GetByIdAsync(id, cancellationToken) is null)
            throw new NotFoundException("Order", id);

        var history = await _orderRepository.GetStatusHistoryAsync(id, cancellationToken);
        return history.Select(_mapper.Map<OrderStatusHistoryDto>).ToList();
    }

    public async Task<OrderResponseDto> PayAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await LoadOrderAsync(id, cancellationToken);

        // Симулированная оплата: фиксируем статус и уведомляем систему продуктов о списании резерва.
        order.MarkAsPaid("Payment received (simulated)");
        await PersistTransitionAsync(order, cancellationToken);

        await _eventPublisher.PublishAsync(new OrderPaidEvent
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

        await PublishStatusChangedAsync(order, OrderStatus.Created, OrderStatus.Paid, cancellationToken);

        _logger.LogInformation("Order {OrderId} paid", order.Id);
        return _mapper.Map<OrderResponseDto>(order);
    }

    public async Task<OrderResponseDto> CancelAsync(Guid id, string? reason, CancellationToken cancellationToken = default)
    {
        var order = await LoadOrderAsync(id, cancellationToken);
        var previous = order.Status;

        order.Cancel(reason ?? "Cancelled by request");
        await PersistTransitionAsync(order, cancellationToken);

        await _eventPublisher.PublishAsync(new OrderCancelledEvent
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

        await PublishStatusChangedAsync(order, previous, OrderStatus.Cancelled, cancellationToken);

        _logger.LogInformation("Order {OrderId} cancelled. Reason: {Reason}", order.Id, reason);
        return _mapper.Map<OrderResponseDto>(order);
    }

    public async Task<OrderResponseDto> ChangeStatusAsync(Guid id, ChangeStatusDto dto, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<OrderStatus>(dto.Status, true, out var targetStatus))
            throw new BusinessRuleException($"Unknown order status '{dto.Status}'");

        var order = await LoadOrderAsync(id, cancellationToken);
        var previous = order.Status;

        order.ChangeStatus(targetStatus, dto.Comment);
        await PersistTransitionAsync(order, cancellationToken);

        await PublishStatusChangedAsync(order, previous, targetStatus, cancellationToken);

        _logger.LogInformation("Order {OrderId} status changed from {From} to {To}", order.Id, previous, targetStatus);
        return _mapper.Map<OrderResponseDto>(order);
    }

    private async Task<Order> LoadOrderAsync(Guid id, CancellationToken cancellationToken) =>
        await _orderRepository.GetByIdAsync(id, cancellationToken)
        ?? throw new NotFoundException("Order", id);

    /// <summary>Сохраняет последний переход статуса заказа и инвалидирует кеш.</summary>
    private async Task PersistTransitionAsync(Order order, CancellationToken cancellationToken)
    {
        var lastHistory = order.StatusHistory.Last();
        await _orderRepository.UpdateStatusAsync(order, lastHistory, cancellationToken);
        await _cache.RemoveAsync(order.Id, cancellationToken);
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
