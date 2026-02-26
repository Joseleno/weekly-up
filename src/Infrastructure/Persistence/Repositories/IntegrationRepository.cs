using Microsoft.EntityFrameworkCore;

using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.Interfaces.Repositories;

namespace WeeklyUp.Infrastructure.Persistence.Repositories;

public sealed class IntegrationRepository : IIntegrationRepository
{
    private const int TokenExpirationThresholdHours = 23;

    private readonly ApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTime;

    public IntegrationRepository(ApplicationDbContext context, IDateTimeProvider dateTime)
    {
        _context = context;
        _dateTime = dateTime;
    }

    public async Task<Integration?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Integrations
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<IReadOnlyList<Integration>> GetByUserIdAsync(
        Guid userId, CancellationToken ct = default)
    {
        var list = await _context.Integrations
            .AsNoTracking()
            .Where(i => i.UserId == userId)
            .ToListAsync(ct);
        return list.AsReadOnly();
    }

    public async Task<IReadOnlyList<Integration>> GetActiveWithExpiredTokensAsync(
        CancellationToken ct = default)
    {
        DateTime threshold = _dateTime.UtcNow.AddHours(-TokenExpirationThresholdHours);
        var list = await _context.Integrations
            .AsNoTracking()
            .Where(i => i.Status == IntegrationStatus.Connected && i.UpdatedAt < threshold)
            .ToListAsync(ct);
        return list.AsReadOnly();
    }

    public void Update(Integration integration) =>
        _context.Integrations.Update(integration);
}
