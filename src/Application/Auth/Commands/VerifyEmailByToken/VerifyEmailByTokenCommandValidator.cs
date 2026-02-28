using FluentValidation;

namespace WeeklyUp.Application.Auth.Commands.VerifyEmailByToken;

public sealed class VerifyEmailByTokenCommandValidator : AbstractValidator<VerifyEmailByTokenCommand>
{
    public VerifyEmailByTokenCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .MaximumLength(500);
    }
}
