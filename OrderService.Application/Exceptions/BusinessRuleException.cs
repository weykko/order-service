namespace OrderService.Application.Exceptions;

/// <summary>
/// Нарушение бизнес-правила уровня приложения (конфликт состояния, недоступность ресурса и т.п.).
/// </summary>
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message)
    {
    }
}
