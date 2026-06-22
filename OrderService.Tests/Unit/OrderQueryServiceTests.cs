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
}
