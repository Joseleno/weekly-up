using Microsoft.EntityFrameworkCore;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.Interfaces.Repositories;

namespace WeeklyUp.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<User?> GetByIdWithIntegrationsAsync(Guid id, CancellationToken ct = default) =>
        await _context.Users
            .Include(u => u.Integrations)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.Value == email.ToLowerInvariant().Trim(), ct);

    public async Task<User?> GetByExternalAuthIdAsync(string externalId, CancellationToken ct = default) =>
        await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.ExternalAuthId == externalId, ct);

    public async Task<IReadOnlyList<User>> GetActiveUsersForReportAsync(
        DayOfWeekPreference day, CancellationToken ct = default)
    {
        var list = await _context.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.IsEmailVerified)
            .Join(_context.ReportPreferences,
                u => u.Id,
                rp => rp.UserId,
                (u, rp) => new { User = u, Preference = rp })
            .Where(x => x.Preference.SendDay == day)
            .Select(x => x.User)
            .ToListAsync(ct);
        return list.AsReadOnly();
    }

    public async Task AddAsync(User user, CancellationToken ct = default) =>
        await _context.Users.AddAsync(user, ct);

    public void Update(User user) =>
        _context.Users.Update(user);
}
