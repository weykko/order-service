namespace OrderService.Application.Abstractions;

/// <summary>
/// Издатель доменных событий во внешнюю шину сообщений.
/// Система заказов выступает источником событий, поэтому контракт ограничен публикацией.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : class;
}
