using FluentValidation;

namespace WeeklyUp.Application.Users.Queries.GetUserProfile;

public sealed class GetUserProfileQueryValidator : AbstractValidator<GetUserProfileQuery>
{
    public GetUserProfileQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId é obrigatório.");
    }
}
