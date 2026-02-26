using Mediator;

using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Application.Common.Mappings;
using WeeklyUp.Domain.Interfaces.Repositories;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Reports.Queries.GetReportHistory;

public sealed class GetReportHistoryQueryHandler
    : IQueryHandler<GetReportHistoryQuery, Result<PagedListDto<ReportSummaryDto>>>
{
    // TODO(Fase4): substituir por GetPagedAsync no repositório quando Infrastructure for implementada.
    // Atualmente carrega até MaxHistoryCount registros em memória e pagina no lado do servidor.
    // TotalCount reflete apenas os registros retornados, não o total real no banco.
    private const int MaxHistoryCount = 500;

    private readonly IReportRepository _reports;

    public GetReportHistoryQueryHandler(IReportRepository reports) =>
        _reports = reports;

    public async ValueTask<Result<PagedListDto<ReportSummaryDto>>> Handle(
        GetReportHistoryQuery query,
        CancellationToken cancellationToken)
    {
        var all = await _reports.GetHistoryAsync(query.UserId, MaxHistoryCount, cancellationToken);
        var totalCount = all.Count;
        var items = all
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => r.ToSummaryDto())
            .ToList();

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
