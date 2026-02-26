using FluentValidation;
using WeeklyUp.Domain.Enums;

namespace WeeklyUp.Application.Users.Commands.UpgradePlan;

public sealed class UpgradePlanCommandValidator : AbstractValidator<UpgradePlanCommand>
{
    public UpgradePlanCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.NewPlan)
            .IsInEnum()
            .NotEqual(PlanType.Free)
            .WithMessage("Novo plano nao pode ser Free.");
    }
}
