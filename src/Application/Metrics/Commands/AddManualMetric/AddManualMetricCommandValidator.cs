using FluentValidation;

namespace WeeklyUp.Application.Metrics.Commands.AddManualMetric;

public sealed class AddManualMetricCommandValidator : AbstractValidator<AddManualMetricCommand>
{
    public AddManualMetricCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId nao pode ser vazio.");

        RuleFor(x => x.Revenue)
            .GreaterThanOrEqualTo(0).When(x => x.Revenue.HasValue)
            .WithMessage("Revenue nao pode ser negativo.");

        RuleFor(x => x.SalesCount)
            .GreaterThanOrEqualTo(0).When(x => x.SalesCount.HasValue)
            .WithMessage("SalesCount nao pode ser negativo.");

        RuleFor(x => x.NewCustomers)
            .GreaterThanOrEqualTo(0).When(x => x.NewCustomers.HasValue)
            .WithMessage("NewCustomers nao pode ser negativo.");

        RuleFor(x => x.Visits)
            .GreaterThanOrEqualTo(0).When(x => x.Visits.HasValue)
            .WithMessage("Visits nao pode ser negativo.");
    }
}
