using FluentValidation;
using OrderService.Application.DTOs;

namespace OrderService.Application.Validators;

/// <summary>
/// Валидация запроса на оформление заказа.
/// </summary>
public class CreateOrderValidator : AbstractValidator<CreateOrderDto>
{
    /// <summary>Поддерживаемые валюты заказа.</summary>
    private static readonly string[] SupportedCurrencies = { "RUB", "USD" };

    public CreateOrderValidator()
    {
        // Валюта необязательна: при отсутствии применяется значение по умолчанию (RUB).
        // Если значение задано — оно должно входить в список поддерживаемых валют.
        RuleFor(x => x.Currency)
            .Must(BeASupportedCurrency)
            .When(x => !string.IsNullOrWhiteSpace(x.Currency))
            .WithMessage($"Currency must be one of: {string.Join(", ", SupportedCurrencies)}");

        RuleFor(x => x.Customer)
            .NotNull().WithMessage("Customer info is required")
            .SetValidator(new CustomerInfoValidator());

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Order must contain at least one item");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .NotEmpty().WithMessage("ProductId is required");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be positive");
        });
    }

    private static bool BeASupportedCurrency(string? currency) =>
        currency is not null && SupportedCurrencies.Contains(currency.ToUpperInvariant());
}

/// <summary>
/// Валидация контактных данных покупателя.
/// </summary>
public class CustomerInfoValidator : AbstractValidator<CustomerInfoDto>
{
    public CustomerInfoValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Customer name is required")
            .MaximumLength(200);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email is invalid");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone is required")
            .MaximumLength(30);

        RuleFor(x => x.ShippingAddress)
            .NotEmpty().WithMessage("Shipping address is required")
            .MaximumLength(500);
    }
}
