using Microsoft.Extensions.Logging;
using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.Interfaces.Repositories;
using WeeklyUp.Domain.ValueObjects;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Infrastructure.DataSources.Manual;

public sealed class ManualDataProvider : IDataSourceProvider
{
    private readonly IManualMetricRepository _repository;
    private readonly ILogger<ManualDataProvider> _logger;

    public IntegrationProvider ProviderType => IntegrationProvider.Manual;

    public ManualDataProvider(IManualMetricRepository repository, ILogger<ManualDataProvider> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<ReportMetrics>> GetMetricsAsync(
        Guid userId, string providerAccountId, string? propertyId, DateRange weekRange, CancellationToken ct = default)
    {
        try
        {
            ManualMetric? metric = await _repository.GetByUserAndWeekAsync(userId, weekRange.Start, ct);
            if (metric is null)
            {
                return AppError.NotFound("Manual.NoData", "Nenhuma metrica manual encontrada para a semana.");
            }

            return MapToReportMetrics(metric);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Erro ao buscar metricas manuais para usuario {UserId}", userId);
            return AppError.Failure("Manual.Error", ex.Message);
        }
    }

    public Task<Result<Demographics?>> GetDemographicsAsync(
        Guid userId, string providerAccountId, string? propertyId, DateRange weekRange, CancellationToken ct = default) =>
        Task.FromResult(Result.Success<Demographics?>(null));

    private static ReportMetrics MapToReportMetrics(ManualMetric metric)
    {
        decimal revenue = metric.Revenue ?? 0m;
        int salesCount = metric.SalesCount ?? 0;
        decimal avgTicket = salesCount > 0 ? revenue / salesCount : 0m;

        return new ReportMetrics(
            revenue: Money.BRL(revenue),
            salesCount: salesCount,
            averageTicket: Money.BRL(avgTicket),
            newCustomers: metric.NewCustomers ?? 0,
            totalVisits: metric.Visits ?? 0,
            uniqueVisitors: metric.Visits ?? 0,
            pageViews: 0,
            topPage: null,
            topTrafficSource: "Manual",
            previousRevenue: null,
            previousVisits: null);
    }
}
