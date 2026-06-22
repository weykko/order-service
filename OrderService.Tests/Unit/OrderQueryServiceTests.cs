using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OrderService.Application.Abstractions;
using OrderService.Application.DTOs;
using OrderService.Application.Exceptions;
using OrderService.Application.Mappings;
using OrderService.Application.Services;
using OrderService.Application.Validators;
using OrderService.Domain.Enums;
using OrderService.Domain.Interfaces;
using OrderService.Domain.Models;
using OrderService.Tests.Helpers;
using Xunit;

namespace OrderService.Tests.Unit;

/// <summary>
/// Тесты сценариев чтения заказов (<see cref="OrderQueryService"/>).
/// </summary>
public class OrderQueryServiceTests
{
    private readonly Mock<IOrderRepository> _repository = new(MockBehavior.Strict);
    private readonly Mock<IOrderCache> _cache = new(MockBehavior.Strict);
    private readonly OrderQueryService _sut;

    public OrderQueryServiceTests()
    {
        var mapper = new MapperConfiguration(
            cfg => cfg.AddProfile<OrderMappingProfile>(),
            NullLoggerFactory.Instance).CreateMapper();

        _sut = new OrderQueryService(_repository.Object, _cache.Object, new OrderFilterValidator(), mapper);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCached_ShouldReturnFromCache_WithoutRepository()
    {
        var id = Guid.NewGuid();
        var cached = new OrderResponseDto { Id = id, Status = nameof(OrderStatus.Created) };
        _cache.Setup(c => c.GetAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(cached);

        var result = await _sut.GetByIdAsync(id);

        result.Should().BeSameAs(cached);
        _repository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ShouldThrowNotFound()
    {
        var id = Guid.NewGuid();
        _cache.Setup(c => c.GetAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((OrderResponseDto?)null);
        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        var act = () => _sut.GetByIdAsync(id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotCached_ShouldLoadFromRepository_AndPopulateCache()
    {
        var order = OrderFactory.CreateOrder();
        _cache.Setup(c => c.GetAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync((OrderResponseDto?)null);
        _repository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _cache.Setup(c => c.SetAsync(order.Id, It.IsAny<OrderResponseDto>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.GetByIdAsync(order.Id);

        result.Id.Should().Be(order.Id);
        _cache.Verify(c => c.SetAsync(order.Id, It.IsAny<OrderResponseDto>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetFilteredAsync_ShouldComposePagedResult_FromListAndCount()
    {
        var filter = new OrderFilterDto { Status = nameof(OrderStatus.Created), Page = 2, PageSize = 5 };
        var orders = new[] { OrderFactory.CreateOrder(), OrderFactory.CreateOrder() };

        _repository.Setup(r => r.GetListAsync(It.IsAny<OrderQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);
        _repository.Setup(r => r.CountAsync(It.IsAny<OrderQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);

        var result = await _sut.GetFilteredAsync(filter);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(7);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(5);
        result.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task GetFilteredAsync_WithInvalidPaging_ShouldThrowValidation()
    {
        var filter = new OrderFilterDto { Page = 0, PageSize = 5 };

        var act = () => _sut.GetFilteredAsync(filter);

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }

    [Fact]
    public async Task GetStatusHistoryAsync_WhenExists_ShouldReturnMappedHistory()
    {
        var order = OrderFactory.CreateOrder();
        _repository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _repository.Setup(r => r.GetStatusHistoryAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order.StatusHistory.ToList());

        var result = await _sut.GetStatusHistoryAsync(order.Id);

        result.Should().ContainSingle();
        result.First().ToStatus.Should().Be(nameof(OrderStatus.Created));
    }

    [Fact]
    public async Task GetStatusHistoryAsync_WhenOrderNotFound_ShouldThrowNotFound()
    {
        var id = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        var act = () => _sut.GetStatusHistoryAsync(id);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
