namespace OrderService.Domain.Exceptions;

/// <summary>
/// Нарушение доменного инварианта или бизнес-правила агрегата.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
