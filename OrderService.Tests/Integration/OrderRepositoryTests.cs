using FluentAssertions;
using OrderService.Domain.Enums;
using OrderService.Domain.Interfaces;
using OrderService.Domain.Models;
using OrderService.Infrastructure.Repositories;
using OrderService.Tests.Helpers;
using Xunit;

namespace OrderService.Tests.Integration;

/// <summary>
/// Интеграционные тесты <see cref="OrderRepository"/> на реальном PostgreSQL.
/// </summary>
[Collection(PostgresCollection.Name)]
public class OrderRepositoryTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private readonly OrderRepository _repository;

    public OrderRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _repository = new OrderRepository(_fixture.ConnectionFactory);
    }

    public Task InitializeAsync() => _fixture.CleanupAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AddAsync_ThenGetById_ShouldReturnOrderWithItemsAndHistory()
    {
        var order = OrderFactory.CreateOrder(itemCount: 2, price: 150m, quantity: 3);

        await _repository.AddAsync(order);
        var loaded = await _repository.GetByIdAsync(order.Id);

        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(order.Id);
        loaded.Status.Should().Be(OrderStatus.Created);
        loaded.Items.Should().HaveCount(2);
        loaded.TotalAmount.Amount.Should().Be(order.TotalAmount.Amount);
        loaded.StatusHistory.Should().ContainSingle();
        loaded.Customer.Email.Should().Be(order.Customer.Email);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldPersistStatusAndAppendHistory()
    {
        var order = OrderFactory.CreateOrder();
        await _repository.AddAsync(order);

        order.MarkAsPaid("paid");
        await _repository.UpdateStatusAsync(order, order.StatusHistory.Last());

        var loaded = await _repository.GetByIdAsync(order.Id);
        loaded!.Status.Should().Be(OrderStatus.Paid);

        var history = await _repository.GetStatusHistoryAsync(order.Id);
        history.Should().HaveCount(2);
        history.Last().ToStatus.Should().Be(OrderStatus.Paid);
    }

    [Fact]
    public async Task GetListAsync_ShouldFilterByStatus_AndPaginate()
    {
        var created = OrderFactory.CreateOrder();
        var paid = OrderFactory.CreateOrder();
        await _repository.AddAsync(created);
        await _repository.AddAsync(paid);

        paid.MarkAsPaid();
        await _repository.UpdateStatusAsync(paid, paid.StatusHistory.Last());

        var query = new OrderQuery(OrderStatus.Paid, null, Page: 1, PageSize: 10);
        var result = await _repository.GetListAsync(query);
        var count = await _repository.CountAsync(query);

        result.Should().ContainSingle().Which.Id.Should().Be(paid.Id);
        count.Should().Be(1);
    }

    [Fact]
    public async Task GetListAsync_ShouldFilterByCustomerEmail()
    {
        var order = OrderFactory.CreateOrder();
        await _repository.AddAsync(order);

        var query = new OrderQuery(null, "ivan@example.com", Page: 1, PageSize: 10);
        var result = await _repository.GetListAsync(query);

        result.Should().ContainSingle().Which.Id.Should().Be(order.Id);
    }
}
