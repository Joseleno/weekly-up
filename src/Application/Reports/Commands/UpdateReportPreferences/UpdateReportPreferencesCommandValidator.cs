using FluentValidation;

namespace WeeklyUp.Application.Reports.Commands.UpdateReportPreferences;

public sealed class UpdateReportPreferencesCommandValidator : AbstractValidator<UpdateReportPreferencesCommand>
{
    public UpdateReportPreferencesCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId nao pode ser vazio.");

        RuleFor(x => x.SendDay)
            .IsInEnum()
            .WithMessage("Dia de envio invalido.");

        RuleFor(x => x.SendTime)
            .Must(t => t.Hour >= 6 && t.Hour <= 22)
            .WithMessage("Horario deve estar entre 06:00 e 22:00.");

        RuleFor(x => x.EnabledSections)
            .NotNull().WithMessage("EnabledSections nao pode ser nulo.");
    }
}
