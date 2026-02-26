using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Enums;

namespace WeeklyUp.Domain.Interfaces.Repositories;

public interface IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    public Task<User?> GetByIdWithIntegrationsAsync(Guid id, CancellationToken ct = default);
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    public Task<User?> GetByExternalAuthIdAsync(string externalId, CancellationToken ct = default);
    public Task<IReadOnlyList<User>> GetActiveUsersForReportAsync(DayOfWeekPreference day, CancellationToken ct = default);
    public Task AddAsync(User user, CancellationToken ct = default);
    public void Update(User user);
}
