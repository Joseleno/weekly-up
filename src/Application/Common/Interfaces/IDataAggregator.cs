using WeeklyUp.Domain.ValueObjects;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Common.Interfaces;

public interface IDataAggregator
{
    public Task<Result<ReportMetrics>> GetAggregatedMetricsAsync(
        Guid userId, DateRange weekRange, CancellationToken ct = default);
}
