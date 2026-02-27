using Mediator;

using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Application.Common.Mappings;
using WeeklyUp.Domain.Interfaces.Repositories;
using WeeklyUp.Shared.Constants;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Reports.Queries.GetReportDetail;

public sealed class GetReportDetailQueryHandler
    : IQueryHandler<GetReportDetailQuery, Result<ReportDetailDto>>
{
    private static readonly TimeSpan _cacheExpiry = TimeSpan.FromHours(1);

    private readonly IReportRepository _reports;
    private readonly IApplicationCacheService _cache;

    public GetReportDetailQueryHandler(
        IReportRepository reports,
        IApplicationCacheService cache)
    {
        _reports = reports;
        _cache = cache;
    }

    public async ValueTask<Result<ReportDetailDto>> Handle(
        GetReportDetailQuery query,
        CancellationToken cancellationToken)
    {
        var report = await _reports.GetByIdAsync(query.ReportId, cancellationToken);
        if (report is null || report.UserId != query.UserId)
        {
            return AppError.NotFound("Report.NotFound", "Relatório não encontrado.");
        }

        var cacheKey = CacheKeys.Report(query.ReportId);
        var cached = await _cache.GetAsync<ReportDetailDto>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var dto = report.ToDetailDto();
        await _cache.SetAsync(cacheKey, dto, _cacheExpiry, cancellationToken);
        return dto;
    }
}
