using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.ValueObjects;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Common.Interfaces;

public interface IDataSourceProvider
{
    public IntegrationProvider ProviderType { get; }

    public Task<Result<ReportMetrics>> GetMetricsAsync(
        Guid userId,
        string providerAccountId,
        string? propertyId,
        DateRange weekRange,
        CancellationToken ct = default);

    public Task<Result<Demographics?>> GetDemographicsAsync(
        Guid userId,
        string providerAccountId,
        string? propertyId,
        DateRange weekRange,
        CancellationToken ct = default);
}
