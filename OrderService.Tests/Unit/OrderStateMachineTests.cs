using FluentAssertions;
using OrderService.Domain.Enums;
using OrderService.Domain.Exceptions;
using OrderService.Tests.Helpers;
using Xunit;

namespace OrderService.Tests.Unit;

/// <summary>
/// Тесты стейт-машины заказа и инвариантов агрегата.
/// </summary>
public class OrderStateMachineTests
{
    [Fact]
    public void NewOrder_ShouldStartInCreatedStatus_WithInitialHistoryEntry()
    {
        var order = OrderFactory.CreateOrder();

        order.Status.Should().Be(OrderStatus.Created);
        order.StatusHistory.Should().ContainSingle()
            .Which.ToStatus.Should().Be(OrderStatus.Created);
    }

    [Fact]
    public void CreateOrder_WithoutItems_ShouldThrow()
    {
        var act = () => new Domain.Models.Order(OrderFactory.CreateCustomer(), Array.Empty<Domain.Models.OrderItem>());

        act.Should().Throw<DomainException>()
            .WithMessage("*at least one item*");
    }

    [Fact]
    public void FullHappyPath_ToReceived_ShouldPassThroughAllStatuses()
    {
        var order = OrderFactory.CreateOrder();

        order.MarkAsPaid();
        order.StartAssembling();
        order.Ship();
        order.Deliver();
        order.Receive();

        order.Status.Should().Be(OrderStatus.Received);
        order.StatusHistory.Select(h => h.ToStatus).Should().ContainInOrder(
            OrderStatus.Created, OrderStatus.Paid, OrderStatus.Assembling,
            OrderStatus.Shipped, OrderStatus.Delivered, OrderStatus.Received);
    }

    [Fact]
    public void Delivered_CanBeReturned()
    {
        var order = OrderFactory.CreateOrder();
        order.MarkAsPaid();
        order.StartAssembling();
        order.Ship();
        order.Deliver();

        order.Return("defective");

        order.Status.Should().Be(OrderStatus.Returned);
    }

    [Theory]
    [InlineData(OrderStatus.Created)]
    [InlineData(OrderStatus.Paid)]
    public void Cancel_ShouldBeAllowed_BeforeAssembling(OrderStatus reachStatus)
    {
        var order = OrderFactory.CreateOrder();
        DriveTo(order, reachStatus);

        order.Cancel("test");

        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_AfterAssembling_ShouldThrow()
    {
        var order = OrderFactory.CreateOrder();
        order.MarkAsPaid();
        order.StartAssembling();

        var act = () => order.Cancel();

        act.Should().Throw<DomainException>()
            .WithMessage("*not allowed*");
    }

    [Fact]
    public void Deliver_FromCreated_ShouldThrow_InvalidTransition()
    {
        var order = OrderFactory.CreateOrder();

        var act = () => order.Deliver();

        act.Should().Throw<DomainException>()
            .WithMessage("*not allowed*");
    }

    [Fact]
    public void ChangeStatus_ToSameStatus_ShouldThrow()
    {
        var order = OrderFactory.CreateOrder();

        var act = () => order.ChangeStatus(OrderStatus.Created);

        act.Should().Throw<DomainException>()
            .WithMessage("*already in status*");
    }

    [Fact]
    public void TotalAmount_ShouldBeSumOfLineTotals()
    {
        var order = OrderFactory.CreateOrder(itemCount: 3, price: 50m, quantity: 2);

        order.TotalAmount.Amount.Should().Be(300m); // 3 * (50 * 2)
    }

    private static void DriveTo(Domain.Models.Order order, OrderStatus target)
    {
        if (target == OrderStatus.Created) return;
        order.MarkAsPaid();
        if (target == OrderStatus.Paid) return;
        order.StartAssembling();
    }
}
