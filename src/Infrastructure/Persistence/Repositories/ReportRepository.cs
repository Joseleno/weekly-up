using Microsoft.EntityFrameworkCore;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Interfaces.Repositories;

namespace WeeklyUp.Infrastructure.Persistence.Repositories;

public sealed class ReportRepository : IReportRepository
{
    private readonly ApplicationDbContext _context;

    public ReportRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Report?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Reports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<Report?> GetByUserAndWeekAsync(
        Guid userId, DateOnly weekStart, CancellationToken ct = default) =>
        await _context.Reports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.UserId == userId && r.WeekRange.Start == weekStart, ct);

    public async Task<IReadOnlyList<Report>> GetHistoryAsync(
        Guid userId, int count = 12, CancellationToken ct = default)
    {
        var list = await _context.Reports
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.WeekRange.Start)
            .Take(count)
            .ToListAsync(ct);
        return list.AsReadOnly();
    }

    public async Task<Report?> GetLatestAsync(Guid userId, CancellationToken ct = default) =>
        await _context.Reports
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.WeekRange.Start)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(Report report, CancellationToken ct = default) =>
        await _context.Reports.AddAsync(report, ct);

    public void Update(Report report) =>
        _context.Reports.Update(report);
}
