using Mediator;
using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Reports.Queries.GetReportDetail;

public sealed record GetReportDetailQuery(
    Guid UserId,
    Guid ReportId) : IQuery<Result<ReportDetailDto>>;
