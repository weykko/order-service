using FluentAssertions;
using OrderService.Domain.Exceptions;
using OrderService.Domain.ValueObjects;
using Xunit;

namespace OrderService.Tests.Unit;

/// <summary>
/// Тесты value object <see cref="CustomerInfo"/>: валидация контактных данных.
/// </summary>
public class CustomerInfoTests
{
    private const string ValidName = "Иван Иванов";
    private const string ValidEmail = "ivan@example.com";
    private const string ValidPhone = "+79990000000";
    private const string ValidAddress = "Москва, ул. Пушкина, д. 1";

    [Fact]
    public void Ctor_WithValidData_ShouldTrimAndStore()
    {
        // Пробелы по краям имени/адреса/телефона должны обрезаться.
        var info = new CustomerInfo($"  {ValidName} ", ValidEmail, $" {ValidPhone} ", $" {ValidAddress} ");

        info.FullName.Should().Be(ValidName);
        info.Email.Should().Be(ValidEmail);
        info.Phone.Should().Be(ValidPhone);
        info.ShippingAddress.Should().Be(ValidAddress);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_WithEmptyName_ShouldThrow(string name)
    {
        var act = () => new CustomerInfo(name, ValidEmail, ValidPhone, ValidAddress);

        act.Should().Throw<DomainException>().WithMessage("*name*");
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing@domain")]
    [InlineData("@example.com")]
    [InlineData("")]
    public void Ctor_WithInvalidEmail_ShouldThrow(string email)
    {
        var act = () => new CustomerInfo(ValidName, email, ValidPhone, ValidAddress);

        act.Should().Throw<DomainException>().WithMessage("*email*");
    }

    [Fact]
    public void Ctor_WithEmptyPhone_ShouldThrow()
    {
        var act = () => new CustomerInfo(ValidName, ValidEmail, "  ", ValidAddress);

        act.Should().Throw<DomainException>().WithMessage("*phone*");
    }

    [Fact]
    public void Ctor_WithEmptyAddress_ShouldThrow()
    {
        var act = () => new CustomerInfo(ValidName, ValidEmail, ValidPhone, "");

        act.Should().Throw<DomainException>().WithMessage("*address*");
    }

    [Fact]
    public void Equality_ShouldCompareByValue()
    {
        var a = new CustomerInfo(ValidName, ValidEmail, ValidPhone, ValidAddress);
        var b = new CustomerInfo(ValidName, ValidEmail, ValidPhone, ValidAddress);

        a.Equals(b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
        a.Equals(null).Should().BeFalse();
    }
}
