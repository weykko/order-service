using Npgsql;

namespace OrderService.Infrastructure.Persistence;

/// <summary>
/// Фабрика подключений к PostgreSQL.
/// </summary>
public interface IPostgresConnectionFactory
{
    NpgsqlConnection GetConnection();
}
