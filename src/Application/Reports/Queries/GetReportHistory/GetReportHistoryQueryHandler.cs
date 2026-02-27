using Mediator;

using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Application.Common.Mappings;
using WeeklyUp.Domain.Interfaces.Repositories;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Reports.Queries.GetReportHistory;

public sealed class GetReportHistoryQueryHandler
    : IQueryHandler<GetReportHistoryQuery, Result<PagedListDto<ReportSummaryDto>>>
{
    private readonly IReportRepository _reports;

    public GetReportHistoryQueryHandler(IReportRepository reports) =>
        _reports = reports;

    public async ValueTask<Result<PagedListDto<ReportSummaryDto>>> Handle(
        GetReportHistoryQuery query,
        CancellationToken cancellationToken)
    {
        var (reports, totalCount) = await _reports.GetPagedHistoryAsync(
            query.UserId, query.Page, query.PageSize, cancellationToken);

        var items = reports.Select(r => r.ToSummaryDto()).ToList();

        var result = new PagedListDto<ReportSummaryDto>(
            Items: items,
            Page: query.Page,
            PageSize: query.PageSize,
            TotalCount: totalCount,
            HasNextPage: query.Page * query.PageSize < totalCount,
            HasPreviousPage: query.Page > 1);

        return result;
    }
}
