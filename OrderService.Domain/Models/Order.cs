using OrderService.Domain.Common;
using OrderService.Domain.Enums;
using OrderService.Domain.Exceptions;
using OrderService.Domain.ValueObjects;

namespace OrderService.Domain.Models;

/// <summary>
/// Агрегат заказа. Инкапсулирует позиции, контактные данные покупателя,
/// текущий статус и историю его изменений. Все переходы статусов проходят
/// через стейт-машину <see cref="AllowedTransitions"/>.
/// </summary>
public class Order : BaseEntity
{
    private readonly List<OrderItem> _items = new();
    private readonly List<OrderStatusHistory> _statusHistory = new();

    /// <summary>
    /// Разрешённые переходы статусов заказа.
    /// </summary>
    private static readonly IReadOnlyDictionary<OrderStatus, OrderStatus[]> AllowedTransitions =
        new Dictionary<OrderStatus, OrderStatus[]>
        {
            [OrderStatus.Created] = new[] { OrderStatus.Paid, OrderStatus.Cancelled },
            [OrderStatus.Paid] = new[] { OrderStatus.Assembling, OrderStatus.Cancelled },
            [OrderStatus.Assembling] = new[] { OrderStatus.Shipped, OrderStatus.Cancelled },
            [OrderStatus.Shipped] = new[] { OrderStatus.Delivered },
            [OrderStatus.Delivered] = Array.Empty<OrderStatus>(),
            [OrderStatus.Cancelled] = Array.Empty<OrderStatus>()
        };

    public OrderStatus Status { get; private set; }
    public CustomerInfo Customer { get; private set; }
    public string Currency { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    public IReadOnlyCollection<OrderStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    public Money TotalAmount =>
        _items.Aggregate(Money.Zero(Currency), (total, item) => total.Add(item.LineTotal));

    private Order()
    {
        Customer = null!;
        Currency = Money.DefaultCurrency;
    }

    public Order(CustomerInfo customer, IEnumerable<OrderItem> items, string currency = Money.DefaultCurrency)
    {
        Customer = customer ?? throw new DomainException("Customer info is required");

        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException("Currency is required");

        Currency = currency.ToUpperInvariant();

        var itemList = items?.ToList() ?? new List<OrderItem>();
        if (itemList.Count == 0)
            throw new DomainException("Order must contain at least one item");

        foreach (var item in itemList)
        {
            item.AttachToOrder(Id);
            _items.Add(item);
        }

        Status = OrderStatus.Created;
        RecordStatusChange(null, OrderStatus.Created, "Order created");
    }

    /// <summary>Помечает заказ оплаченным.</summary>
    public void MarkAsPaid(string? comment = null) => ChangeStatus(OrderStatus.Paid, comment);

    /// <summary>Переводит заказ в сборку.</summary>
    public void StartAssembling(string? comment = null) => ChangeStatus(OrderStatus.Assembling, comment);

    /// <summary>Передаёт заказ в доставку.</summary>
    public void Ship(string? comment = null) => ChangeStatus(OrderStatus.Shipped, comment);

    /// <summary>Помечает заказ доставленным.</summary>
    public void Deliver(string? comment = null) => ChangeStatus(OrderStatus.Delivered, comment);

    /// <summary>Отменяет заказ, если это допустимо текущим статусом.</summary>
    public void Cancel(string? comment = null) => ChangeStatus(OrderStatus.Cancelled, comment);

    /// <summary>
    /// Универсальный переход в произвольный целевой статус с валидацией по стейт-машине.
    /// </summary>
    public void ChangeStatus(OrderStatus targetStatus, string? comment = null)
    {
        if (Status == targetStatus)
            throw new DomainException($"Order is already in status '{targetStatus}'");

        if (!CanTransitionTo(targetStatus))
            throw new DomainException($"Transition from '{Status}' to '{targetStatus}' is not allowed");

        var previous = Status;
        Status = targetStatus;
        Touch();
        RecordStatusChange(previous, targetStatus, comment);
    }

    public bool CanTransitionTo(OrderStatus targetStatus) =>
        AllowedTransitions.TryGetValue(Status, out var allowed) && allowed.Contains(targetStatus);

    private void RecordStatusChange(OrderStatus? from, OrderStatus to, string? comment) =>
        _statusHistory.Add(new OrderStatusHistory(Id, from, to, comment));

    /// <summary>
    /// Восстановление агрегата из хранилища (используется инфраструктурным слоем).
    /// </summary>
    public static Order Rehydrate(
        Guid id,
        CustomerInfo customer,
        string currency,
        OrderStatus status,
        DateTime createdAt,
        DateTime? updatedAt,
        IEnumerable<OrderItem> items,
        IEnumerable<OrderStatusHistory> history)
    {
        var order = new Order
        {
            Id = id,
            Customer = customer,
            Currency = currency,
            Status = status,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        order._items.AddRange(items);
        order._statusHistory.AddRange(history);
        return order;
    }
}
