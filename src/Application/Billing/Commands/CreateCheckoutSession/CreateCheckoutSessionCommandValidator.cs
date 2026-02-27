using FluentValidation;

using WeeklyUp.Domain.Enums;

namespace WeeklyUp.Application.Billing.Commands.CreateCheckoutSession;

public sealed class CreateCheckoutSessionCommandValidator
    : AbstractValidator<CreateCheckoutSessionCommand>
{
    public CreateCheckoutSessionCommandValidator()
    {
        RuleFor(c => c.UserId)
            .NotEmpty().WithMessage("UserId nao pode ser vazio.");

        RuleFor(c => c.Plan)
            .NotEqual(PlanType.Free).WithMessage("Plano Free nao requer checkout.");

        RuleFor(c => c.SuccessUrl)
            .NotEmpty().WithMessage("SuccessUrl nao pode ser vazia.")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
            .WithMessage("SuccessUrl deve ser uma URL HTTPS valida.");

        RuleFor(c => c.CancelUrl)
            .NotEmpty().WithMessage("CancelUrl nao pode ser vazia.")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
            .WithMessage("CancelUrl deve ser uma URL HTTPS valida.");
    }
}
