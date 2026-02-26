using Microsoft.Extensions.Logging;

using WeeklyUp.Domain.Interfaces.Repositories;

namespace WeeklyUp.Infrastructure.BackgroundJobs;

public sealed class IntegrationSyncJob
{
    private readonly IIntegrationRepository _integrations;
    private readonly ILogger<IntegrationSyncJob> _logger;

    public IntegrationSyncJob(
        IIntegrationRepository integrations,
        ILogger<IntegrationSyncJob> logger)
    {
        _integrations = integrations;
        _logger = logger;
    }

    public Task ExecuteAsync(CancellationToken ct = default)
    {
        // TODO: Implement full sync logic with IDataSourceProvider in future iteration
        _logger.LogInformation("IntegrationSyncJob executado - sincronizacao pendente de implementacao completa");
        return Task.CompletedTask;
    }
}
