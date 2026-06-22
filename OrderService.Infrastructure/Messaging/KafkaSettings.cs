namespace OrderService.Infrastructure.Messaging;

/// <summary>
/// Настройки подключения к Kafka.
/// </summary>
public class KafkaSettings
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string ClientId { get; set; } = "order-service";
    public string GroupId { get; set; } = "order-service-group";
}
