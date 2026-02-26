using FluentValidation;

namespace WeeklyUp.Application.Integrations.Commands.ConnectIntegration;

public sealed class ConnectIntegrationCommandValidator : AbstractValidator<ConnectIntegrationCommand>
{
    public ConnectIntegrationCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId nao pode ser vazio.");

        RuleFor(x => x.AccessToken)
            .NotEmpty().WithMessage("AccessToken nao pode ser vazio.");

        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("AccountId nao pode ser vazio.");
    }
}
