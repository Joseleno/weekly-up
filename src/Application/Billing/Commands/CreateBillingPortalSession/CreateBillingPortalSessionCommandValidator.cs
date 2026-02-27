using FluentValidation;

namespace WeeklyUp.Application.Billing.Commands.CreateBillingPortalSession;

public sealed class CreateBillingPortalSessionCommandValidator
    : AbstractValidator<CreateBillingPortalSessionCommand>
{
    public CreateBillingPortalSessionCommandValidator()
    {
        RuleFor(c => c.UserId)
            .NotEmpty().WithMessage("UserId nao pode ser vazio.");

        RuleFor(c => c.ReturnUrl)
            .NotEmpty().WithMessage("ReturnUrl nao pode ser vazia.")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
            .WithMessage("ReturnUrl deve ser uma URL HTTPS valida.");
    }
}
