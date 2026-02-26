using FluentValidation;

namespace WeeklyUp.Application.Reports.Queries.GetReportHistory;

public sealed class GetReportHistoryQueryValidator : AbstractValidator<GetReportHistoryQuery>
{
    public GetReportHistoryQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId é obrigatório.");

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page deve ser maior ou igual a 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50)
            .WithMessage("PageSize deve estar entre 1 e 50.");
    }
}
