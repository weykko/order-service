using FluentMigrator;

namespace OrderService.Infrastructure.Migrations;

/// <summary>
/// Начальная схема системы заказов: заказы, позиции и история статусов.
/// </summary>
[Migration(20240601000001, "Initial order schema")]
public class InitialMigration : Migration
{
    public override void Up()
    {
        Create.Table("orders")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("status").AsInt32().NotNullable()
            .WithColumn("currency").AsString(3).NotNullable()
            .WithColumn("customer_full_name").AsString(200).NotNullable()
            .WithColumn("customer_email").AsString(256).NotNullable()
            .WithColumn("customer_phone").AsString(30).NotNullable()
            .WithColumn("shipping_address").AsString(500).NotNullable()
            .WithColumn("total_amount").AsDecimal(14, 2).NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("updated_at").AsDateTime().Nullable();

        Create.Table("order_items")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("order_id").AsGuid().NotNullable().ForeignKey("orders", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("product_id").AsGuid().NotNullable()
            .WithColumn("product_name").AsString(200).NotNullable()
            .WithColumn("unit_price").AsDecimal(14, 2).NotNullable()
            .WithColumn("currency").AsString(3).NotNullable()
            .WithColumn("quantity").AsInt32().NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        Create.Table("order_status_history")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("order_id").AsGuid().NotNullable().ForeignKey("orders", "id").OnDelete(System.Data.Rule.Cascade)
            .WithColumn("from_status").AsInt32().Nullable()
            .WithColumn("to_status").AsInt32().NotNullable()
            .WithColumn("comment").AsString(500).Nullable()
            .WithColumn("changed_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        Create.Index("idx_orders_status").OnTable("orders").OnColumn("status");
        Create.Index("idx_orders_customer_email").OnTable("orders").OnColumn("customer_email");
        Create.Index("idx_order_items_order").OnTable("order_items").OnColumn("order_id");
        Create.Index("idx_status_history_order").OnTable("order_status_history").OnColumn("order_id");
    }

    public override void Down()
    {
        Delete.Table("order_status_history");
        Delete.Table("order_items");
        Delete.Table("orders");
    }
}
