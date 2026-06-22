using OrderService.Application.DTOs;

namespace OrderService.Application.Abstractions;

/// <summary>
/// Управление жизненным циклом заказа: переходы статусов с сохранением истории,
/// публикацией событий и возвратом денег там, где это требуется.
/// </summary>
public interface IOrderLifecycleService
{
    /// <summary>Симулированная оплата: Created → Paid, публикует событие оплаты.</summary>
    Task<OrderResponseDto> PayAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Paid → Assembling (заказ собирается).</summary>
    Task<OrderResponseDto> AssembleAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Assembling → Shipped (передан в доставку).</summary>
    Task<OrderResponseDto> ShipAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Shipped → Delivered (доставлен в ПВЗ).</summary>
    Task<OrderResponseDto> DeliverAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Delivered → Received (получатель забрал заказ, заказ завершён).</summary>
    Task<OrderResponseDto> ReceiveAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Delivered → Returned (произведён возврат, деньги возвращаются).</summary>
    Task<OrderResponseDto> ReturnAsync(Guid id, string? reason, CancellationToken cancellationToken = default);

    /// <summary>Отменяет заказ (возможно только до сборки); для оплаченного — возврат денег.</summary>
    Task<OrderResponseDto> CancelAsync(Guid id, string? reason, CancellationToken cancellationToken = default);

    /// <summary>Произвольный переход статуса через стейт-машину заказа.</summary>
    Task<OrderResponseDto> ChangeStatusAsync(Guid id, ChangeStatusDto dto, CancellationToken cancellationToken = default);
}
