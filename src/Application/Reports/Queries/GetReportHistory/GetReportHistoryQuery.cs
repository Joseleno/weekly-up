using Mediator;

using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Reports.Queries.GetReportHistory;

public sealed record GetReportHistoryQuery(
    Guid UserId,
    int Page = 1,
    int PageSize = 10) : IQuery<Result<PagedListDto<ReportSummaryDto>>>;
