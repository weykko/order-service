using AutoMapper;
using OrderService.Application.DTOs;
using OrderService.Domain.Models;
using OrderService.Domain.ValueObjects;

namespace OrderService.Application.Mappings;

/// <summary>
/// Профиль преобразования доменных моделей заказа в DTO ответов.
/// </summary>
public class OrderMappingProfile : Profile
{
    public OrderMappingProfile()
    {
        CreateMap<CustomerInfo, CustomerInfoDto>();

        CreateMap<OrderItem, OrderItemDto>()
            .ForMember(dest => dest.UnitPrice, opt => opt.MapFrom(src => src.UnitPrice.Amount))
            .ForMember(dest => dest.LineTotal, opt => opt.MapFrom(src => src.LineTotal.Amount));

        CreateMap<Order, OrderResponseDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.TotalAmount, opt => opt.MapFrom(src => src.TotalAmount.Amount));

        CreateMap<OrderStatusHistory, OrderStatusHistoryDto>()
            .ForMember(dest => dest.FromStatus, opt => opt.MapFrom(src => src.FromStatus.HasValue ? src.FromStatus.ToString() : null))
            .ForMember(dest => dest.ToStatus, opt => opt.MapFrom(src => src.ToStatus.ToString()));
    }
}
