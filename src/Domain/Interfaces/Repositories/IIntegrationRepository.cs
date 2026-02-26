using WeeklyUp.Domain.Entities;

namespace WeeklyUp.Domain.Interfaces.Repositories;

public interface IIntegrationRepository
{
    public Task<Integration?> GetByIdAsync(Guid id, CancellationToken ct = default);
    public Task<IReadOnlyList<Integration>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    public Task<IReadOnlyList<Integration>> GetActiveWithExpiredTokensAsync(CancellationToken ct = default);
}
