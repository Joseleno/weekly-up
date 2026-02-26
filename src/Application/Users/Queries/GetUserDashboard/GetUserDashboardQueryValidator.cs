using FluentValidation;

namespace WeeklyUp.Application.Users.Queries.GetUserDashboard;

public sealed class GetUserDashboardQueryValidator : AbstractValidator<GetUserDashboardQuery>
{
    public GetUserDashboardQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId é obrigatório.");
    }
}
