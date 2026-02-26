using Microsoft.EntityFrameworkCore;

using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Interfaces.Repositories;

namespace WeeklyUp.Infrastructure.Persistence.Repositories;

public sealed class ManualMetricRepository : IManualMetricRepository
{
    private readonly ApplicationDbContext _context;

    public ManualMetricRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ManualMetric?> GetByUserAndWeekAsync(
        Guid userId, DateOnly weekStart, CancellationToken ct = default) =>
        await _context.ManualMetrics
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == userId && m.WeekStart == weekStart, ct);

    public async Task AddAsync(ManualMetric metric, CancellationToken ct = default) =>
        await _context.ManualMetrics.AddAsync(metric, ct);

    public void Update(ManualMetric metric) =>
        _context.ManualMetrics.Update(metric);
}
