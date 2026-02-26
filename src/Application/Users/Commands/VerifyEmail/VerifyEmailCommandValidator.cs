using FluentValidation;

namespace WeeklyUp.Application.Users.Commands.VerifyEmail;

public sealed class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Token)
            .NotEmpty();
    }
}
