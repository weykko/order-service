using Dapper;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Infrastructure.Migrations;
using OrderService.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace OrderService.Tests.Integration;

/// <summary>
/// Поднимает контейнер PostgreSQL и применяет реальные FluentMigrator-миграции,
/// чтобы тесты работали на актуальной схеме БД.
/// </summary>
public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("orderservice_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public IPostgresConnectionFactory ConnectionFactory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        var connectionString = _container.GetConnectionString();
        ConnectionFactory = new PostgresConnectionFactory(connectionString);

        RunMigrations(connectionString);
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    public async Task CleanupAsync()
    {
        await using var connection = ConnectionFactory.GetConnection();
        await connection.ExecuteAsync(
            "TRUNCATE TABLE order_status_history, order_items, orders RESTART IDENTITY CASCADE;");
    }

    private static void RunMigrations(string connectionString)
    {
        var serviceProvider = new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof(InitialMigration).Assembly).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole())
            .BuildServiceProvider(false);

        using var scope = serviceProvider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMigrationRunner>().MigrateUp();
    }
}

[CollectionDefinition(Name)]
public class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "Postgres collection";
}
