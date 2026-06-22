using AutoMapper;
using FluentValidation;
using OrderService.Application.Abstractions;
using OrderService.Application.DTOs;
using OrderService.Application.Exceptions;
using OrderService.Domain.Enums;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Services;

/// <summary>
/// Сценарии чтения заказов. Чтение по идентификатору проходит через кеш (read-through).
/// </summary>
public class OrderQueryService : IOrderQueryService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly IOrderRepository _orderRepository;
    private readonly IOrderCache _cache;
    private readonly IValidator<OrderFilterDto> _filterValidator;
    private readonly IMapper _mapper;

    public OrderQueryService(
        IOrderRepository orderRepository,
        IOrderCache cache,
        IValidator<OrderFilterDto> filterValidator,
        IMapper mapper)
    {
        _orderRepository = orderRepository;
        _cache = cache;
        _filterValidator = filterValidator;
        _mapper = mapper;
    }

    public async Task<OrderResponseDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cached = await _cache.GetAsync(id, cancellationToken);
        if (cached != null)
            return cached;

        var order = await _orderRepository.GetRequiredAsync(id, cancellationToken);
        var dto = _mapper.Map<OrderResponseDto>(order);

        await _cache.SetAsync(id, dto, CacheTtl, cancellationToken);
        return dto;
    }

    public async Task<PagedResult<OrderResponseDto>> GetFilteredAsync(OrderFilterDto filter, CancellationToken cancellationToken = default)
    {
        await _filterValidator.ValidateAndThrowAsync(filter, cancellationToken);

        OrderStatus? status = string.IsNullOrWhiteSpace(filter.Status)
            ? null
            : Enum.Parse<OrderStatus>(filter.Status, true);

        var query = new OrderQuery(status, filter.CustomerEmail, filter.Page, filter.PageSize);

        var orders = await _orderRepository.GetListAsync(query, cancellationToken);
        var totalCount = await _orderRepository.CountAsync(query, cancellationToken);

        return new PagedResult<OrderResponseDto>
        {
            Items = orders.Select(_mapper.Map<OrderResponseDto>).ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<IReadOnlyCollection<OrderStatusHistoryDto>> GetStatusHistoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (await _orderRepository.GetByIdAsync(id, cancellationToken) is null)
            throw new NotFoundException("Order", id);

        var history = await _orderRepository.GetStatusHistoryAsync(id, cancellationToken);
        return history.Select(_mapper.Map<OrderStatusHistoryDto>).ToList();
    }
}
