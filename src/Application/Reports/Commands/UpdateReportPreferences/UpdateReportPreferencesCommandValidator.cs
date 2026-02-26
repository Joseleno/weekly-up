using FluentValidation;

namespace WeeklyUp.Application.Reports.Commands.UpdateReportPreferences;

public sealed class UpdateReportPreferencesCommandValidator : AbstractValidator<UpdateReportPreferencesCommand>
{
    public UpdateReportPreferencesCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId nao pode ser vazio.");

        RuleFor(x => x.EnabledSections)
            .NotNull().WithMessage("EnabledSections nao pode ser nulo.");
    }
}
