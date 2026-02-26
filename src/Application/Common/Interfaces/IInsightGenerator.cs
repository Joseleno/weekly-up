using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.ValueObjects;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Common.Interfaces;

public interface IInsightGenerator
{
    public Task<Result<ReportInsights>> GenerateAsync(
        ReportMetrics metrics,
        BusinessType businessType,
        string language,
        CancellationToken ct = default);
}
