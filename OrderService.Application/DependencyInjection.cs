using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Application.Abstractions;
using OrderService.Application.Mappings;
using OrderService.Application.Services;
using OrderService.Application.Validators;

namespace OrderService.Application;

/// <summary>
/// Регистрация сервисов прикладного слоя в DI-контейнере.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg => cfg.AddMaps(typeof(OrderMappingProfile).Assembly));
        services.AddValidatorsFromAssemblyContaining<CreateOrderValidator>();

        services.AddScoped<IOrderCreationService, OrderCreationService>();
        services.AddScoped<IOrderQueryService, OrderQueryService>();
        services.AddScoped<IOrderLifecycleService, OrderLifecycleService>();
        return services;
    }
}
