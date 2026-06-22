using Npgsql;

namespace OrderService.Infrastructure.Persistence;

public class PostgresConnectionFactory(string connectionString) : IPostgresConnectionFactory
{
    public NpgsqlConnection GetConnection() => new(connectionString);
}
