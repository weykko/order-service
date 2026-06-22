using FluentAssertions;
using OrderService.Domain.Exceptions;
using OrderService.Domain.Models;
using OrderService.Domain.ValueObjects;
using Xunit;

namespace OrderService.Tests.Unit;

/// <summary>
/// Тесты позиции заказа <see cref="OrderItem"/>: инварианты и расчёт суммы строки.
/// </summary>
public class OrderItemTests
{
    private static readonly Money Price = new(100m, "RUB");

    [Fact]
    public void Ctor_WithValidData_ShouldComputeLineTotal()
    {
        var item = new OrderItem(Guid.NewGuid(), "Phone", Price, 3);

        item.LineTotal.Amount.Should().Be(300m);
        item.Quantity.Should().Be(3);
    }

    [Fact]
    public void Ctor_WithEmptyProductId_ShouldThrow()
    {
        var act = () => new OrderItem(Guid.Empty, "Phone", Price, 1);

        act.Should().Throw<DomainException>().WithMessage("*ProductId*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_WithEmptyName_ShouldThrow(string name)
    {
        var act = () => new OrderItem(Guid.NewGuid(), name, Price, 1);

        act.Should().Throw<DomainException>().WithMessage("*name*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Ctor_WithNonPositiveQuantity_ShouldThrow(int quantity)
    {
        var act = () => new OrderItem(Guid.NewGuid(), "Phone", Price, quantity);

        act.Should().Throw<DomainException>().WithMessage("*quantity*");
    }

    [Fact]
    public void Ctor_WithNullPrice_ShouldThrow()
    {
        var act = () => new OrderItem(Guid.NewGuid(), "Phone", null!, 1);

        act.Should().Throw<DomainException>().WithMessage("*price*");
    }

    [Fact]
    public void Rehydrate_ShouldRestoreIdentityAndFields()
    {
        var id = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddDays(-1);

        var item = OrderItem.Rehydrate(id, orderId, productId, "Phone", Price, 2, createdAt);

        item.Id.Should().Be(id);
        item.OrderId.Should().Be(orderId);
        item.ProductId.Should().Be(productId);
        item.Quantity.Should().Be(2);
        item.CreatedAt.Should().Be(createdAt);
    }
}
