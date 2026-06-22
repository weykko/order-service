using System.Text.RegularExpressions;
using OrderService.Domain.Exceptions;

namespace OrderService.Domain.ValueObjects;

/// <summary>
/// Контактные данные и адрес доставки покупателя. Иммутабельный value object.
/// </summary>
public sealed partial class CustomerInfo : IEquatable<CustomerInfo>
{
    public string FullName { get; }
    public string Email { get; }
    public string Phone { get; }
    public string ShippingAddress { get; }

    public CustomerInfo(string fullName, string email, string phone, string shippingAddress)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("Customer name is required");

        if (string.IsNullOrWhiteSpace(email) || !EmailRegex().IsMatch(email))
            throw new DomainException("Customer email is invalid");

        if (string.IsNullOrWhiteSpace(phone))
            throw new DomainException("Customer phone is required");

        if (string.IsNullOrWhiteSpace(shippingAddress))
            throw new DomainException("Shipping address is required");

        FullName = fullName.Trim();
        Email = email.Trim();
        Phone = phone.Trim();
        ShippingAddress = shippingAddress.Trim();
    }

    public bool Equals(CustomerInfo? other)
    {
        if (other is null) return false;
        return FullName == other.FullName
               && Email == other.Email
               && Phone == other.Phone
               && ShippingAddress == other.ShippingAddress;
    }

    public override bool Equals(object? obj) => Equals(obj as CustomerInfo);

    public override int GetHashCode() => HashCode.Combine(FullName, Email, Phone, ShippingAddress);

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}
