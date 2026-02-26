using WeeklyUp.Domain.Entities;

namespace WeeklyUp.Domain.Interfaces.Repositories;

public interface IManualMetricRepository
{
    public Task<ManualMetric?> GetByUserAndWeekAsync(Guid userId, DateOnly weekStart, CancellationToken ct = default);
    public Task AddAsync(ManualMetric metric, CancellationToken ct = default);
    public void Update(ManualMetric metric);
}
