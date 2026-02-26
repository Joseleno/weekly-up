using WeeklyUp.Domain.Interfaces.Repositories;

namespace WeeklyUp.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    public IUserRepository Users { get; }
    public IIntegrationRepository Integrations { get; }
    public IReportRepository Reports { get; }
    public IManualMetricRepository ManualMetrics { get; }
    public IReportPreferenceRepository ReportPreferences { get; }
    public Task<int> SaveChangesAsync(CancellationToken ct = default);
}
