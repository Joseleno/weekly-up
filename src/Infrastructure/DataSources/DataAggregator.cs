using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.Interfaces.Repositories;
using WeeklyUp.Domain.ValueObjects;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Infrastructure.DataSources;

public sealed class DataAggregator : IDataAggregator
{
    private readonly IEnumerable<IDataSourceProvider> _providers;
    private readonly IIntegrationRepository _integrations;

    public DataAggregator(
        IEnumerable<IDataSourceProvider> providers,
        IIntegrationRepository integrations)
    {
        _providers = providers;
        _integrations = integrations;
    }

    public async Task<Result<ReportMetrics>> GetAggregatedMetricsAsync(
        Guid userId, DateRange weekRange, CancellationToken ct = default)
    {
        var integrations = await _integrations.GetByUserIdAsync(userId, ct);
        var active = integrations
            .Where(i => i.Status == IntegrationStatus.Connected)
            .ToList();

        if (active.Count == 0)
        {
            return AppError.NotFound("DataAggregator.NoIntegrations", "Nenhuma integracao ativa encontrada.");
        }

        return await FetchMetricsFromProviders(userId, active, weekRange, ct);
    }

    private async Task<Result<ReportMetrics>> FetchMetricsFromProviders(
        Guid userId,
        IReadOnlyList<Domain.Entities.Integration> integrations,
        DateRange weekRange,
        CancellationToken ct)
    {
        foreach (var integration in integrations)
        {
            var provider = _providers.FirstOrDefault(p => p.ProviderType == integration.Provider);
            if (provider is null)
            {
                continue;
            }

            var result = await provider.GetMetricsAsync(
                userId, integration.ProviderAccountId, integration.PropertyId, weekRange, ct);
            if (result.IsSuccess)
            {
                return result;
            }
        }

        return AppError.Failure("DataAggregator.NoData", "Nenhum provedor retornou dados.");
    }
}
