using WeeklyUp.Domain.Interfaces;
using WeeklyUp.Domain.Interfaces.Repositories;

namespace WeeklyUp.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public IUserRepository Users { get; }
    public IIntegrationRepository Integrations { get; }
    public IReportRepository Reports { get; }
    public IManualMetricRepository ManualMetrics { get; }
    public IReportPreferenceRepository ReportPreferences { get; }

    public UnitOfWork(
        ApplicationDbContext context,
        IUserRepository users,
        IIntegrationRepository integrations,
        IReportRepository reports,
        IManualMetricRepository manualMetrics,
        IReportPreferenceRepository reportPreferences)
    {
        _context = context;
        Users = users;
        Integrations = integrations;
        Reports = reports;
        ManualMetrics = manualMetrics;
        ReportPreferences = reportPreferences;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        _context.SaveChangesAsync(ct);

    public void Dispose() => _context.Dispose();
}
