using FluentValidation;

namespace WeeklyUp.Application.Auth.Commands.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(255);

        RuleFor(x => x.ExternalAuthId)
            .NotEmpty()
            .MaximumLength(500);
    }
}
