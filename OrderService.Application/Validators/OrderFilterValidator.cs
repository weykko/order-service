using FluentValidation;
using OrderService.Application.DTOs;
using OrderService.Domain.Enums;

namespace OrderService.Application.Validators;

/// <summary>
/// Валидация параметров фильтрации и пагинации списка заказов.
/// </summary>
public class OrderFilterValidator : AbstractValidator<OrderFilterDto>
{
    private const int MaxPageSize = 100;

    public OrderFilterValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize)
            .WithMessage($"PageSize must be between 1 and {MaxPageSize}");

        RuleFor(x => x.Status)
            .Must(BeAValidStatus)
            .When(x => !string.IsNullOrWhiteSpace(x.Status))
            .WithMessage("Unknown order status");
    }

    private static bool BeAValidStatus(string? status) =>
        Enum.TryParse<OrderStatus>(status, true, out _);
}
