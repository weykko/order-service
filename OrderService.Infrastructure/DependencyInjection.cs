using FluentMigrator.Runner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Application.Abstractions;
using OrderService.Domain.Interfaces;
using OrderService.Infrastructure.Cache;
using OrderService.Infrastructure.Http;
using OrderService.Infrastructure.Messaging;
using OrderService.Infrastructure.Persistence;
using OrderService.Infrastructure.Repositories;
using StackExchange.Redis;

namespace OrderService.Infrastructure;

/// <summary>
/// Регистрация инфраструктурных зависимостей: БД и миграции, кеш, шина событий,
/// HTTP-клиент системы продуктов.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        AddPersistence(services, configuration);
        AddCache(services, configuration);
        AddMessaging(services, configuration);
        AddProductCatalog(services, configuration);
        return services;
    }

    private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PostgreSQL")
            ?? throw new InvalidOperationException("PostgreSQL connection string is not configured");

        services.AddSingleton<IPostgresConnectionFactory>(new PostgresConnectionFactory(connectionString));
        services.AddScoped<IOrderRepository, OrderRepository>();

        // Сами миграции выполняются отдельным MigrationRunner при старте хоста.
    }

    private static void AddCache(IServiceCollection services, IConfiguration configuration)
    {
        var redisConnection = configuration.GetValue<string>("Redis:ConnectionString")
            ?? throw new InvalidOperationException("Redis connection string is not configured");

        services.AddMemoryCache();
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
        services.AddSingleton<IOrderCache, RedisOrderCache>();
    }

    private static void AddMessaging(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaSettings>(configuration.GetSection("Kafka"));
        services.AddSingleton<IEventPublisher, KafkaEventPublisher>();
    }

    private static void AddProductCatalog(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ProductCatalogOptions>(configuration.GetSection(ProductCatalogOptions.SectionName));

        var options = configuration.GetSection(ProductCatalogOptions.SectionName).Get<ProductCatalogOptions>()
            ?? new ProductCatalogOptions();

        services.AddHttpClient<IProductCatalogClient, ProductCatalogClient>(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });
    }
}
