using OrderService.Domain.Models;
using OrderService.Domain.ValueObjects;

namespace OrderService.Tests.Helpers;

/// <summary>
/// Фабрика тестовых данных для агрегата заказа.
/// </summary>
public static class OrderFactory
{
    public static CustomerInfo CreateCustomer() =>
        new("Иван Иванов", "ivan@example.com", "+79990000000", "Москва, ул. Пушкина, д. 1");

    public static OrderItem CreateItem(decimal price = 100m, int quantity = 1, string currency = "RUB") =>
        new(Guid.NewGuid(), "Test Product", new Money(price, currency), quantity);

    public static Order CreateOrder(int itemCount = 1, decimal price = 100m, int quantity = 1, string currency = "RUB")
    {
        var items = Enumerable.Range(0, itemCount)
            .Select(_ => CreateItem(price, quantity, currency))
            .ToList();

        return new Order(CreateCustomer(), items, currency);
    }
}
