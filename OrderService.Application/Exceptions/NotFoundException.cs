namespace OrderService.Application.Exceptions;

/// <summary>
/// Запрашиваемый ресурс не найден.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string resource, object key)
        : base($"{resource} with key '{key}' was not found")
    {
    }

    public NotFoundException(string message) : base(message)
    {
    }
}
