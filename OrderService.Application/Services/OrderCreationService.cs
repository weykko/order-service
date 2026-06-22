using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using OrderService.Application.Abstractions;
using OrderService.Application.DTOs;
using OrderService.Application.Events;
using OrderService.Application.Exceptions;
using OrderService.Domain.Interfaces;
using OrderService.Domain.Models;
using OrderService.Domain.ValueObjects;

namespace OrderService.Application.Services;

/// <summary>
/// Сценарий оформления заказа: валидация, проверка и синхронный резерв товара
/// в системе продуктов, фиксация цены на момент покупки, сохранение и публикация события.
/// </summary>
public class OrderCreationService : IOrderCreationService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductCatalogClient _catalogClient;
    private readonly IEventPublisher _eventPublisher;
    private readonly IValidator<CreateOrderDto> _validator;
    private readonly IMapper _mapper;
    private readonly ILogger<OrderCreationService> _logger;

    public OrderCreationService(
        IOrderRepository orderRepository,
        IProductCatalogClient catalogClient,
        IEventPublisher eventPublisher,
        IValidator<CreateOrderDto> validator,
        IMapper mapper,
        ILogger<OrderCreationService> logger)
    {
        _orderRepository = orderRepository;
        _catalogClient = catalogClient;
        _eventPublisher = eventPublisher;
        _validator = validator;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<OrderResponseDto> CreateAsync(CreateOrderDto dto, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(dto, cancellationToken);

        var customer = new CustomerInfo(dto.Customer.FullName, dto.Customer.Email, dto.Customer.Phone, dto.Customer.ShippingAddress);
        var currency = string.IsNullOrWhiteSpace(dto.Currency)
            ? Money.DefaultCurrency
            : dto.Currency.ToUpperInvariant();

        var orderItems = await BuildReservedItemsAsync(dto.Items, cancellationToken);

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

    /// <summary>
    /// Для каждой позиции получает товар из каталога, резервирует склад и
    /// формирует позицию заказа с ценой на момент покупки.
    /// </summary>
    private async Task<List<OrderItem>> BuildReservedItemsAsync(
        IEnumerable<CreateOrderItemDto> lines, CancellationToken cancellationToken)
    {
        var orderItems = new List<OrderItem>();

        foreach (var line in lines)
        {
            var product = await _catalogClient.GetProductAsync(line.ProductId, cancellationToken)
                          ?? throw new NotFoundException("Product", line.ProductId);

            if (!await _catalogClient.ReserveStockAsync(line.ProductId, line.Quantity, cancellationToken))
                throw new BusinessRuleException(
                    $"Insufficient stock for product '{product.Name}' ({line.ProductId}). Requested: {line.Quantity}");

            var unitPrice = new Money(product.Price, product.Currency);
            orderItems.Add(new OrderItem(product.Id, product.Name, unitPrice, line.Quantity));
        }

        return orderItems;
    }
}
