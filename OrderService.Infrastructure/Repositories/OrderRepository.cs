using Dapper;
using Npgsql;
using OrderService.Domain.Enums;
using OrderService.Domain.Interfaces;
using OrderService.Domain.Models;
using OrderService.Domain.ValueObjects;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Infrastructure.Repositories;

/// <summary>
/// Хранилище заказов на Dapper. Заказ, его позиции и история статусов
/// сохраняются и читаются согласованно; запись выполняется в транзакции.
/// </summary>
public class OrderRepository(IPostgresConnectionFactory connectionFactory) : IOrderRepository
{
    private readonly IPostgresConnectionFactory _connectionFactory = connectionFactory;

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.GetConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string insertOrderSql = @"
            INSERT INTO orders
                (id, status, currency, customer_full_name, customer_email, customer_phone,
                 shipping_address, total_amount, created_at, updated_at)
            VALUES
                (@Id, @Status, @Currency, @FullName, @Email, @Phone,
                 @ShippingAddress, @TotalAmount, @CreatedAt, @UpdatedAt)";

        await connection.ExecuteAsync(new CommandDefinition(insertOrderSql, new
        {
            order.Id,
            Status = (int)order.Status,
            order.Currency,
            order.Customer.FullName,
            order.Customer.Email,
            order.Customer.Phone,
            order.Customer.ShippingAddress,
            TotalAmount = order.TotalAmount.Amount,
            order.CreatedAt,
            order.UpdatedAt
        }, transaction, cancellationToken: cancellationToken));

        await InsertItemsAsync(connection, transaction, order, cancellationToken);
        await InsertHistoryAsync(connection, transaction, order.StatusHistory, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.GetConnection();

        const string orderSql = @"
            SELECT id, status, currency, customer_full_name, customer_email, customer_phone,
                   shipping_address, created_at, updated_at
            FROM orders WHERE id = @Id";

        var orderRow = await connection.QuerySingleOrDefaultAsync(
            new CommandDefinition(orderSql, new { Id = id }, cancellationToken: cancellationToken));

        if (orderRow is null)
            return null;

        var items = await LoadItemsAsync(connection, id, cancellationToken);
        var history = await LoadHistoryAsync(connection, id, cancellationToken);

        return MapOrder(orderRow, items, history);
    }

    public async Task<IReadOnlyCollection<Order>> GetListAsync(OrderQuery query, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.GetConnection();

        var (whereSql, parameters) = BuildFilter(query);
        var sql = $@"
            SELECT id, status, currency, customer_full_name, customer_email, customer_phone,
                   shipping_address, created_at, updated_at
            FROM orders
            {whereSql}
            ORDER BY created_at DESC
            LIMIT @PageSize OFFSET @Offset";

        parameters.Add("PageSize", query.PageSize);
        parameters.Add("Offset", query.Offset);

        var rows = (await connection.QueryAsync(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken))).ToList();

        if (rows.Count == 0)
            return Array.Empty<Order>();

        var orderIds = rows.Select(r => (Guid)r.id).ToList();
        var itemsByOrder = await LoadItemsByOrderIdsAsync(connection, orderIds, cancellationToken);
        var historyByOrder = await LoadHistoryByOrderIdsAsync(connection, orderIds, cancellationToken);

        var orders = new List<Order>(rows.Count);
        foreach (var row in rows)
        {
            var orderId = (Guid)row.id;
            var items = itemsByOrder.TryGetValue(orderId, out var i) ? i : new List<OrderItem>();
            var history = historyByOrder.TryGetValue(orderId, out var h) ? h : new List<OrderStatusHistory>();
            orders.Add(MapOrder(row, items, history));
        }

        return orders;
    }

    public async Task<int> CountAsync(OrderQuery query, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.GetConnection();
        var (whereSql, parameters) = BuildFilter(query);
        var sql = $"SELECT COUNT(*) FROM orders {whereSql}";
        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyCollection<OrderStatusHistory>> GetStatusHistoryAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.GetConnection();
        return await LoadHistoryAsync(connection, orderId, cancellationToken);
    }

    public async Task UpdateStatusAsync(Order order, OrderStatusHistory newHistoryEntry, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.GetConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string updateSql = @"
            UPDATE orders
            SET status = @Status, updated_at = @UpdatedAt
            WHERE id = @Id";

        await connection.ExecuteAsync(new CommandDefinition(updateSql, new
        {
            order.Id,
            Status = (int)order.Status,
            UpdatedAt = order.UpdatedAt ?? DateTime.UtcNow
        }, transaction, cancellationToken: cancellationToken));

        await InsertHistoryAsync(connection, transaction, new[] { newHistoryEntry }, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task InsertItemsAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Order order, CancellationToken cancellationToken)
    {
        const string sql = @"
            INSERT INTO order_items
                (id, order_id, product_id, product_name, unit_price, currency, quantity, created_at)
            VALUES
                (@Id, @OrderId, @ProductId, @ProductName, @UnitPrice, @Currency, @Quantity, @CreatedAt)";

        foreach (var item in order.Items)
        {
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                item.Id,
                OrderId = order.Id,
                item.ProductId,
                item.ProductName,
                UnitPrice = item.UnitPrice.Amount,
                Currency = item.UnitPrice.Currency,
                item.Quantity,
                item.CreatedAt
            }, transaction, cancellationToken: cancellationToken));
        }
    }

    private static async Task InsertHistoryAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, IEnumerable<OrderStatusHistory> history, CancellationToken cancellationToken)
    {
        const string sql = @"
            INSERT INTO order_status_history
                (id, order_id, from_status, to_status, comment, changed_at)
            VALUES
                (@Id, @OrderId, @FromStatus, @ToStatus, @Comment, @ChangedAt)";

        foreach (var entry in history)
        {
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                entry.Id,
                entry.OrderId,
                FromStatus = entry.FromStatus.HasValue ? (int?)entry.FromStatus.Value : null,
                ToStatus = (int)entry.ToStatus,
                entry.Comment,
                entry.ChangedAt
            }, transaction, cancellationToken: cancellationToken));
        }
    }

    private static async Task<List<OrderItem>> LoadItemsAsync(NpgsqlConnection connection, Guid orderId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT id, order_id, product_id, product_name, unit_price, currency, quantity, created_at
            FROM order_items WHERE order_id = @OrderId";

        var rows = await connection.QueryAsync(new CommandDefinition(sql, new { OrderId = orderId }, cancellationToken: cancellationToken));

        var items = new List<OrderItem>();
        foreach (var row in rows)
            items.Add(MapItem(row));

        return items;
    }

    private static async Task<List<OrderStatusHistory>> LoadHistoryAsync(NpgsqlConnection connection, Guid orderId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT id, order_id, from_status, to_status, comment, changed_at
            FROM order_status_history WHERE order_id = @OrderId
            ORDER BY changed_at ASC";

        var rows = await connection.QueryAsync(new CommandDefinition(sql, new { OrderId = orderId }, cancellationToken: cancellationToken));

        var history = new List<OrderStatusHistory>();
        foreach (var row in rows)
            history.Add(MapHistory(row));

        return history;
    }

    private static async Task<Dictionary<Guid, List<OrderItem>>> LoadItemsByOrderIdsAsync(NpgsqlConnection connection, List<Guid> orderIds, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT id, order_id, product_id, product_name, unit_price, currency, quantity, created_at
            FROM order_items WHERE order_id = ANY(@OrderIds)";

        var rows = await connection.QueryAsync(new CommandDefinition(sql, new { OrderIds = orderIds }, cancellationToken: cancellationToken));

        var result = new Dictionary<Guid, List<OrderItem>>();
        foreach (var row in rows)
        {
            var orderId = (Guid)row.order_id;
            if (!result.TryGetValue(orderId, out var list))
            {
                list = new List<OrderItem>();
                result[orderId] = list;
            }

            list.Add(MapItem(row));
        }

        return result;
    }

    private static async Task<Dictionary<Guid, List<OrderStatusHistory>>> LoadHistoryByOrderIdsAsync(NpgsqlConnection connection, List<Guid> orderIds, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT id, order_id, from_status, to_status, comment, changed_at
            FROM order_status_history WHERE order_id = ANY(@OrderIds)
            ORDER BY changed_at ASC";

        var rows = await connection.QueryAsync(new CommandDefinition(sql, new { OrderIds = orderIds }, cancellationToken: cancellationToken));

        var result = new Dictionary<Guid, List<OrderStatusHistory>>();
        foreach (var row in rows)
        {
            var orderId = (Guid)row.order_id;
            if (!result.TryGetValue(orderId, out var list))
            {
                list = new List<OrderStatusHistory>();
                result[orderId] = list;
            }

            list.Add(MapHistory(row));
        }

        return result;
    }

    private static (string WhereSql, DynamicParameters Parameters) BuildFilter(OrderQuery query)
    {
        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        if (query.Status.HasValue)
        {
            conditions.Add("status = @Status");
            parameters.Add("Status", (int)query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.CustomerEmail))
        {
            conditions.Add("customer_email ILIKE @CustomerEmail");
            parameters.Add("CustomerEmail", $"%{query.CustomerEmail}%");
        }

        var whereSql = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;
        return (whereSql, parameters);
    }

    private static Order MapOrder(dynamic row, List<OrderItem> items, List<OrderStatusHistory> history)
    {
        var customer = new CustomerInfo(
            (string)row.customer_full_name,
            (string)row.customer_email,
            (string)row.customer_phone,
            (string)row.shipping_address);

        return Order.Rehydrate(
            (Guid)row.id,
            customer,
            (string)row.currency,
            (OrderStatus)(int)row.status,
            (DateTime)row.created_at,
            row.updated_at is null ? null : (DateTime?)row.updated_at,
            items,
            history);
    }

    private static OrderItem MapItem(dynamic row) =>
        OrderItem.Rehydrate(
            (Guid)row.id,
            (Guid)row.order_id,
            (Guid)row.product_id,
            (string)row.product_name,
            new Money((decimal)row.unit_price, (string)row.currency),
            (int)row.quantity,
            (DateTime)row.created_at);

    private static OrderStatusHistory MapHistory(dynamic row)
    {
        int? from = row.from_status is null ? null : (int?)row.from_status;
        return OrderStatusHistory.Rehydrate(
            (Guid)row.id,
            (Guid)row.order_id,
            from.HasValue ? (OrderStatus)from.Value : null,
            (OrderStatus)(int)row.to_status,
            row.comment is null ? null : (string)row.comment,
            (DateTime)row.changed_at);
    }
}
