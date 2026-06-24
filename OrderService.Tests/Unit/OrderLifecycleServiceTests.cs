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
using OrderService.Domain.Enums;
using OrderService.Domain.Interfaces;
using OrderService.Domain.Models;
using OrderService.Tests.Helpers;
using Xunit;

namespace OrderService.Tests.Unit;

/// <summary>
/// Тесты управления жизненным циклом заказа (<see cref="OrderLifecycleService"/>):
/// переходы статусов, возврат денег и публикация событий.
/// </summary>
public class OrderLifecycleServiceTests
{
    private readonly Mock<IOrderRepository> _repository = new(MockBehavior.Strict);
    private readonly Mock<IOrderCache> _cache = new(MockBehavior.Strict);
    private readonly Mock<IEventPublisher> _publisher = new();
    private readonly OrderLifecycleService _sut;

    public OrderLifecycleServiceTests()
    {
        var mapper = new MapperConfiguration(
            cfg => cfg.AddProfile<OrderMappingProfile>(),
            NullLoggerFactory.Instance).CreateMapper();

        _sut = new OrderLifecycleService(
            _repository.Object,
            _cache.Object,
            _publisher.Object,
            mapper,
            NullLogger<OrderLifecycleService>.Instance);

        _repository.Setup(r => r.UpdateStatusAsync(It.IsAny<Order>(), It.IsAny<OrderStatusHistory>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _cache.Setup(c => c.RemoveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task PayAsync_ShouldMoveToPaid_PublishPaidAndStatusEvents()
    {
        var order = OrderFactory.CreateOrder();
        SetupGet(order);

        var result = await _sut.PayAsync(order.Id);

        result.Status.Should().Be(nameof(OrderStatus.Paid));
        _publisher.Verify(b => b.PublishAsync(It.IsAny<OrderPaidEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        _publisher.Verify(b => b.PublishAsync(It.IsAny<OrderStatusChangedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FullHappyPath_ToReceived_ShouldSucceed()
    {
        var order = OrderFactory.CreateOrder();
        SetupGet(order);

        await _sut.PayAsync(order.Id);
        await _sut.AssembleAsync(order.Id);
        await _sut.ShipAsync(order.Id);
        await _sut.DeliverAsync(order.Id);
        var result = await _sut.ReceiveAsync(order.Id);

        result.Status.Should().Be(nameof(OrderStatus.Received));
        _publisher.Verify(b => b.PublishAsync(It.IsAny<OrderRefundedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelAsync_AfterPayment_ShouldThrow_DomainException()
    {
        var order = OrderFactory.CreateOrder();
        SetupGet(order);
        await _sut.PayAsync(order.Id);

        var act = () => _sut.CancelAsync(order.Id, "too late");

        await act.Should().ThrowAsync<Domain.Exceptions.DomainException>();
    }

    [Fact]
    public async Task CancelAsync_BeforePayment_ShouldCancel_AndNotRefund()
    {
        var order = OrderFactory.CreateOrder();
        SetupGet(order);

        var result = await _sut.CancelAsync(order.Id, "changed mind");

        result.Status.Should().Be(nameof(OrderStatus.Cancelled));
        _publisher.Verify(b => b.PublishAsync(It.IsAny<OrderCancelledEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        _publisher.Verify(b => b.PublishAsync(It.IsAny<OrderRefundedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReturnAsync_AfterDelivered_ShouldRefund()
    {
        var order = OrderFactory.CreateOrder();
        SetupGet(order);
        await _sut.PayAsync(order.Id);
        await _sut.AssembleAsync(order.Id);
        await _sut.ShipAsync(order.Id);
        await _sut.DeliverAsync(order.Id);

        var result = await _sut.ReturnAsync(order.Id, "defective");

        result.Status.Should().Be(nameof(OrderStatus.Returned));
        _publisher.Verify(b => b.PublishAsync(It.IsAny<OrderRefundedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangeStatusAsync_WithUnknownStatus_ShouldThrowBusinessRule()
    {
        var act = () => _sut.ChangeStatusAsync(Guid.NewGuid(), new ChangeStatusDto { Status = "Flying" });

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*Unknown order status*");
    }

    [Fact]
    public async Task AssembleShipDeliver_ShouldAdvanceStatuses_AndPublishStatusEvents()
    {
        var order = OrderFactory.CreateOrder();
        SetupGet(order);
        await _sut.PayAsync(order.Id);

        (await _sut.AssembleAsync(order.Id)).Status.Should().Be(nameof(OrderStatus.Assembling));
        (await _sut.ShipAsync(order.Id)).Status.Should().Be(nameof(OrderStatus.Shipped));
        (await _sut.DeliverAsync(order.Id)).Status.Should().Be(nameof(OrderStatus.Delivered));

        // 4 перехода (pay/assemble/ship/deliver) → 4 события смены статуса.
        _publisher.Verify(b => b.PublishAsync(It.IsAny<OrderStatusChangedEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(4));
    }

    [Fact]
    public async Task ChangeStatusAsync_WithValidTransition_ShouldApplyAndPersist()
    {
        var order = OrderFactory.CreateOrder();
        SetupGet(order);

        var result = await _sut.ChangeStatusAsync(order.Id, new ChangeStatusDto { Status = nameof(OrderStatus.Paid) });

        result.Status.Should().Be(nameof(OrderStatus.Paid));
        _repository.Verify(r => r.UpdateStatusAsync(order, It.IsAny<OrderStatusHistory>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangeStatusAsync_WithInvalidTransition_ShouldThrowDomainException()
    {
        var order = OrderFactory.CreateOrder();
        SetupGet(order);

        // Created → Delivered напрямую недопустимо.
        var act = () => _sut.ChangeStatusAsync(order.Id, new ChangeStatusDto { Status = nameof(OrderStatus.Delivered) });

        await act.Should().ThrowAsync<Domain.Exceptions.DomainException>();
    }

    [Fact]
    public async Task PayAsync_WhenOrderNotFound_ShouldThrowNotFound()
    {
        var id = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        var act = () => _sut.PayAsync(id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private void SetupGet(Order order) =>
        _repository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
}
