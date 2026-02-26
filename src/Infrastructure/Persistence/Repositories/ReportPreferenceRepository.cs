using Microsoft.EntityFrameworkCore;

using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Interfaces.Repositories;

namespace WeeklyUp.Infrastructure.Persistence.Repositories;

public sealed class ReportPreferenceRepository : IReportPreferenceRepository
{
    private readonly ApplicationDbContext _context;

    public ReportPreferenceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ReportPreference?> GetByUserIdAsync(
        Guid userId, CancellationToken ct = default) =>
        await _context.ReportPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(rp => rp.UserId == userId, ct);

    public async Task AddAsync(ReportPreference pref, CancellationToken ct = default) =>
        await _context.ReportPreferences.AddAsync(pref, ct);

    public void Update(ReportPreference pref) =>
        _context.ReportPreferences.Update(pref);
}
