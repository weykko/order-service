using AutoMapper;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;
using OrderService.Application.DTOs;
using OrderService.Application.Validators;
using OrderService.Application.Events;
using OrderService.Application.Exceptions;
using OrderService.Application.Mappings;
using OrderService.Application.Abstractions;
using OrderService.Application.Services;
using OrderService.Domain.Enums;
using OrderService.Domain.Interfaces;
using OrderService.Domain.Models;
using OrderService.Tests.Helpers;
using Xunit;

namespace OrderService.Tests.Unit;

/// <summary>
/// Тесты сценариев <see cref="OrderManagementService"/> на моках инфраструктуры.
/// </summary>
public class OrderManagementServiceTests
{
    private readonly Mock<IOrderRepository> _repository = new(MockBehavior.Strict);
    private readonly Mock<IProductCatalogClient> _catalog = new(MockBehavior.Strict);
    private readonly Mock<IOrderCache> _cache = new(MockBehavior.Strict);
    private readonly Mock<IEventPublisher> _publisher = new(MockBehavior.Strict);
    private readonly IMapper _mapper;
    private readonly OrderManagementService _sut;

    public OrderManagementServiceTests()
    {
        _mapper = new MapperConfiguration(
            cfg => cfg.AddProfile<OrderMappingProfile>(),
            NullLoggerFactory.Instance).CreateMapper();
        _sut = new OrderManagementService(
            _repository.Object,
            _catalog.Object,
            _cache.Object,
            _publisher.Object,
            new CreateOrderValidator(),
            new OrderFilterValidator(),
            _mapper,
            NullLogger<OrderManagementService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_WhenStockAvailable_ShouldPersistOrderAndPublishEvent()
    {
        var productId = Guid.NewGuid();
        var dto = new CreateOrderDto
        {
            Currency = "RUB",
            Customer = new CustomerInfoDto
            {
                FullName = "Иван", Email = "ivan@example.com", Phone = "+79990000000", ShippingAddress = "Москва"
            },
            Items = { new CreateOrderItemDto { ProductId = productId, Quantity = 2 } }
        };

        _catalog.Setup(c => c.GetProductAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductInfoDto { Id = productId, Name = "Phone", Price = 100m, Currency = "RUB", IsInStock = true, AvailableStock = 5 });
        _catalog.Setup(c => c.ReserveStockAsync(productId, 2, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _repository.Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _publisher.Setup(b => b.PublishAsync(It.IsAny<OrderCreatedEvent>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(dto);

        result.Status.Should().Be(nameof(OrderStatus.Created));
        result.TotalAmount.Should().Be(200m);
        _repository.Verify(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
        _publisher.Verify(b => b.PublishAsync(It.IsAny<OrderCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenProductNotFound_ShouldThrowNotFound()
    {
        var productId = Guid.NewGuid();
        var dto = BuildValidCreateDto(productId);

        _catalog.Setup(c => c.GetProductAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductInfoDto?)null);

        var act = () => _sut.CreateAsync(dto);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_WhenReserveFails_ShouldThrowBusinessRule()
    {
        var productId = Guid.NewGuid();
        var dto = BuildValidCreateDto(productId);

        _catalog.Setup(c => c.GetProductAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductInfoDto { Id = productId, Name = "Phone", Price = 100m, Currency = "RUB" });
        _catalog.Setup(c => c.ReserveStockAsync(productId, 1, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var act = () => _sut.CreateAsync(dto);

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*Insufficient stock*");
    }

    [Fact]
    public async Task GetByIdAsync_WhenCached_ShouldReturnFromCache_WithoutRepository()
    {
        var id = Guid.NewGuid();
        var cached = new OrderResponseDto { Id = id, Status = nameof(OrderStatus.Created) };
        _cache.Setup(c => c.GetAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(cached);

        var result = await _sut.GetByIdAsync(id);

        result.Should().BeSameAs(cached);
        _repository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ShouldThrowNotFound()
    {
        var id = Guid.NewGuid();
        _cache.Setup(c => c.GetAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((OrderResponseDto?)null);
        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        var act = () => _sut.GetByIdAsync(id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task PayAsync_ShouldMoveToPaid_PublishPaidAndStatusEvents()
    {
        var order = OrderFactory.CreateOrder();
        _repository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _repository.Setup(r => r.UpdateStatusAsync(order, It.IsAny<OrderStatusHistory>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _cache.Setup(c => c.RemoveAsync(order.Id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _publisher.Setup(b => b.PublishAsync(It.IsAny<OrderPaidEvent>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _publisher.Setup(b => b.PublishAsync(It.IsAny<OrderStatusChangedEvent>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _sut.PayAsync(order.Id);

        result.Status.Should().Be(nameof(OrderStatus.Paid));
        _publisher.Verify(b => b.PublishAsync(It.IsAny<OrderPaidEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        _publisher.Verify(b => b.PublishAsync(It.IsAny<OrderStatusChangedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelAsync_ShouldMoveToCancelled_AndPublishCancelledEvent()
    {
        var order = OrderFactory.CreateOrder();
        _repository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _repository.Setup(r => r.UpdateStatusAsync(order, It.IsAny<OrderStatusHistory>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _cache.Setup(c => c.RemoveAsync(order.Id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _publisher.Setup(b => b.PublishAsync(It.IsAny<OrderCancelledEvent>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _publisher.Setup(b => b.PublishAsync(It.IsAny<OrderStatusChangedEvent>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _sut.CancelAsync(order.Id, "changed mind");

        result.Status.Should().Be(nameof(OrderStatus.Cancelled));
        _publisher.Verify(b => b.PublishAsync(It.IsAny<OrderCancelledEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangeStatusAsync_WithUnknownStatus_ShouldThrowBusinessRule()
    {
        var act = () => _sut.ChangeStatusAsync(Guid.NewGuid(), new ChangeStatusDto { Status = "Flying" });

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*Unknown order status*");
    }

    private static CreateOrderDto BuildValidCreateDto(Guid productId) => new()
    {
        Currency = "RUB",
        Customer = new CustomerInfoDto
        {
            FullName = "Иван", Email = "ivan@example.com", Phone = "+79990000000", ShippingAddress = "Москва"
        },
        Items = { new CreateOrderItemDto { ProductId = productId, Quantity = 1 } }
    };
}
