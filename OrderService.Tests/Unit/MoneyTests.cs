using FluentAssertions;
using OrderService.Domain.Exceptions;
using OrderService.Domain.ValueObjects;
using Xunit;

namespace OrderService.Tests.Unit;

/// <summary>
/// Тесты value object <see cref="Money"/>: инварианты и арифметика.
/// </summary>
public class MoneyTests
{
    [Fact]
    public void Ctor_ShouldRoundAmount_AndNormalizeCurrency()
    {
        var money = new Money(10.006m, "rub");

        money.Amount.Should().Be(10.01m);
        money.Currency.Should().Be("RUB");
    }

    [Fact]
    public void Ctor_WithNegativeAmount_ShouldThrow()
    {
        var act = () => new Money(-1m);

        act.Should().Throw<DomainException>().WithMessage("*negative*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_WithEmptyCurrency_ShouldThrow(string currency)
    {
        var act = () => new Money(1m, currency);

        act.Should().Throw<DomainException>().WithMessage("*Currency*");
    }

    [Fact]
    public void Add_SameCurrency_ShouldSumAmounts()
    {
        var result = new Money(10m, "RUB").Add(new Money(5m, "RUB"));

        result.Amount.Should().Be(15m);
        result.Currency.Should().Be("RUB");
    }

    [Fact]
    public void Add_DifferentCurrencies_ShouldThrow()
    {
        var act = () => new Money(10m, "RUB").Add(new Money(5m, "USD"));

        act.Should().Throw<DomainException>().WithMessage("*different currencies*");
    }

    [Fact]
    public void Multiply_ByPositive_ShouldScaleAmount()
    {
        new Money(10m, "RUB").Multiply(3).Amount.Should().Be(30m);
    }

    [Fact]
    public void Multiply_ByNegative_ShouldThrow()
    {
        var act = () => new Money(10m).Multiply(-1);

        act.Should().Throw<DomainException>().WithMessage("*negative*");
    }

    [Fact]
    public void Equality_ShouldCompareByValue()
    {
        var a = new Money(10m, "RUB");
        var b = new Money(10m, "RUB");

        a.Equals(b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
        a.Equals(new Money(11m, "RUB")).Should().BeFalse();
        a.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Zero_ShouldHaveZeroAmount()
    {
        Money.Zero("USD").Amount.Should().Be(0m);
        Money.Zero("USD").Currency.Should().Be("USD");
    }
}
