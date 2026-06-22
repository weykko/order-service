using FluentMigrator.Runner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace OrderService.Infrastructure.Migrations;

/// <summary>
/// Применяет миграции FluentMigrator при старте хоста на отдельном service provider,
/// не засоряя основной DI-контейнер приложения.
/// </summary>
public static class MigrationRunner
{
    private const string ConnectionStringName = "PostgreSQL";

    public static IHost RunMigrations(this IHost host)
    {
        var configuration = host.Services.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException("PostgreSQL connection string is not configured");

        using var scope = CreateMigrationServices(connectionString).CreateScope();
        scope.ServiceProvider.GetRequiredService<IMigrationRunner>().MigrateUp();

        return host;
    }

    private static IServiceProvider CreateMigrationServices(string connectionString) =>
        new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof(InitialMigration).Assembly).For.Migrations())
            .AddLogging(lb => lb.AddFluentMigratorConsole())
            .BuildServiceProvider(false);
}
