using WeeklyUp.Domain.Entities;

namespace WeeklyUp.Domain.Interfaces.Repositories;

public interface IReportRepository
{
    public Task<Report?> GetByIdAsync(Guid id, CancellationToken ct = default);
    public Task<Report?> GetByUserAndWeekAsync(Guid userId, DateOnly weekStart, CancellationToken ct = default);
    public Task<IReadOnlyList<Report>> GetHistoryAsync(Guid userId, int count = 12, CancellationToken ct = default);
    public Task<Report?> GetLatestAsync(Guid userId, CancellationToken ct = default);
    public Task AddAsync(Report report, CancellationToken ct = default);
    public void Update(Report report);
}
