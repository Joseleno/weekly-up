using FluentValidation;

namespace WeeklyUp.Application.Integrations.Queries.GetIntegrations;

public sealed class GetIntegrationsQueryValidator : AbstractValidator<GetIntegrationsQuery>
{
    public GetIntegrationsQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId é obrigatório.");
    }
}
