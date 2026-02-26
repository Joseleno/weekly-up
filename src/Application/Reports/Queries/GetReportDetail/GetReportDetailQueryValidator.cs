using FluentValidation;

namespace WeeklyUp.Application.Reports.Queries.GetReportDetail;

public sealed class GetReportDetailQueryValidator : AbstractValidator<GetReportDetailQuery>
{
    public GetReportDetailQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId é obrigatório.");

        RuleFor(x => x.ReportId)
            .NotEmpty()
            .WithMessage("ReportId é obrigatório.");
    }
}
