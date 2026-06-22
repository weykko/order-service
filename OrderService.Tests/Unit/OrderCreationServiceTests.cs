using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OrderService.Application.Abstractions;
using OrderService.Application.DTOs;
using OrderService.Application.Events;
using OrderService.Application.Exceptions;
using OrderService.Application.Mappings;
using OrderService.Application.Services;
using OrderService.Application.Validators;
using OrderService.Domain.Enums;
using OrderService.Domain.Interfaces;
using OrderService.Domain.Models;
using Xunit;

namespace OrderService.Tests.Unit;

/// <summary>
/// Тесты сценария оформления заказа (<see cref="OrderCreationService"/>).
/// </summary>
public class OrderCreationServiceTests
{
    private readonly Mock<IOrderRepository> _repository = new(MockBehavior.Strict);
    private readonly Mock<IProductCatalogClient> _catalog = new(MockBehavior.Strict);
    private readonly Mock<IEventPublisher> _publisher = new(MockBehavior.Strict);
    private readonly OrderCreationService _sut;

    public OrderCreationServiceTests()
    {
        var mapper = new MapperConfiguration(
            cfg => cfg.AddProfile<OrderMappingProfile>(),
            NullLoggerFactory.Instance).CreateMapper();

        _sut = new OrderCreationService(
            _repository.Object,
            _catalog.Object,
            _publisher.Object,
            new CreateOrderValidator(),
            mapper,
            NullLogger<OrderCreationService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_WhenStockAvailable_ShouldPersistOrderAndPublishEvent()
    {
        var productId = Guid.NewGuid();
        var dto = BuildValidCreateDto(productId, quantity: 2);

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
        _catalog.Setup(c => c.GetProductAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductInfoDto?)null);

        var act = () => _sut.CreateAsync(BuildValidCreateDto(productId));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_WhenReserveFails_ShouldThrowBusinessRule()
    {
        var productId = Guid.NewGuid();
        _catalog.Setup(c => c.GetProductAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductInfoDto { Id = productId, Name = "Phone", Price = 100m, Currency = "RUB" });
        _catalog.Setup(c => c.ReserveStockAsync(productId, 1, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var act = () => _sut.CreateAsync(BuildValidCreateDto(productId));

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*Insufficient stock*");
    }

    private static CreateOrderDto BuildValidCreateDto(Guid productId, int quantity = 1) => new()
    {
        Currency = "RUB",
        Customer = new CustomerInfoDto
        {
            FullName = "Иван", Email = "ivan@example.com", Phone = "+79990000000", ShippingAddress = "Москва"
        },
        Items = { new CreateOrderItemDto { ProductId = productId, Quantity = quantity } }
    };
}
