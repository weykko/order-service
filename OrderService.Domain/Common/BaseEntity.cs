namespace OrderService.Domain.Common;

/// <summary>
/// Базовая сущность с идентификатором и аудит-метками времени.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime? UpdatedAt { get; protected set; }

    protected BaseEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    protected void Touch() => UpdatedAt = DateTime.UtcNow;
}
