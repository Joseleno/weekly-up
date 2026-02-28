using FluentValidation;

namespace WeeklyUp.Application.Billing.Commands.HandleStripeWebhook;

public sealed class HandleStripeWebhookCommandValidator
    : AbstractValidator<HandleStripeWebhookCommand>
{
    public HandleStripeWebhookCommandValidator()
    {
        RuleFor(c => c.WebhookEvent.Type)
            .NotEmpty().WithMessage("Tipo do evento nao pode ser vazio.");

        RuleFor(c => c.WebhookEvent.Id)
            .NotEmpty().WithMessage("Id do evento nao pode ser vazio.");
    }
}
