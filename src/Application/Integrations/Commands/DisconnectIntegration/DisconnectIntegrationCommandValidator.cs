using FluentValidation;

namespace WeeklyUp.Application.Integrations.Commands.DisconnectIntegration;

public sealed class DisconnectIntegrationCommandValidator : AbstractValidator<DisconnectIntegrationCommand>
{
    public DisconnectIntegrationCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId nao pode ser vazio.");
    }
}
