using FluentValidation;

namespace WeeklyUp.Application.Reports.Commands.GenerateWeeklyReport;

public sealed class GenerateWeeklyReportCommandValidator : AbstractValidator<GenerateWeeklyReportCommand>
{
    public GenerateWeeklyReportCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId nao pode ser vazio.");
    }
}
