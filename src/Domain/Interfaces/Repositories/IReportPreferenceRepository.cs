using WeeklyUp.Domain.Entities;

namespace WeeklyUp.Domain.Interfaces.Repositories;

public interface IReportPreferenceRepository
{
    public Task<ReportPreference?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    public Task AddAsync(ReportPreference pref, CancellationToken ct = default);
    public void Update(ReportPreference pref);
}
