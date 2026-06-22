using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderService.Application.Abstractions;

namespace OrderService.Infrastructure.Messaging;

/// <summary>
/// Издатель доменных событий поверх Kafka. Имя топика выводится из имени типа
/// события (без суффикса "Event", в нижнем регистре), тело сериализуется в JSON
/// (camelCase) — контракт совместим с консьюмерами системы продуктов.
/// </summary>
public sealed class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private const string EventSuffix = "Event";

    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventPublisher> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public KafkaEventPublisher(IOptions<KafkaSettings> settings, ILogger<KafkaEventPublisher> logger)
    {
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = settings.Value.BootstrapServers,
            ClientId = settings.Value.ClientId
        };

        _producer = new ProducerBuilder<string, string>(producerConfig).Build();
    }

    public async Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        var topic = ResolveTopic(typeof(TEvent));
        var payload = JsonSerializer.Serialize(domainEvent, _jsonOptions);

        try
        {
            var deliveryResult = await _producer.ProduceAsync(topic, new Message<string, string>
            {
                Key = Guid.NewGuid().ToString(),
                Value = payload
            }, cancellationToken);

            _logger.LogInformation("Event '{Event}' published to topic '{Topic}' at offset {Offset}",
                typeof(TEvent).Name, topic, deliveryResult.Offset);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to publish event '{Event}' to topic '{Topic}'", typeof(TEvent).Name, topic);
            throw;
        }
    }

    private static string ResolveTopic(Type eventType)
    {
        var name = eventType.Name;
        if (name.EndsWith(EventSuffix, StringComparison.Ordinal))
            name = name[..^EventSuffix.Length];

        return name.ToLowerInvariant();
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
