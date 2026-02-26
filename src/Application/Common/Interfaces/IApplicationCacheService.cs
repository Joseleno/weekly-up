namespace WeeklyUp.Application.Common.Interfaces;

public interface IApplicationCacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    public Task SetAsync<T>(string key, T value, TimeSpan expiry, CancellationToken ct = default);
    public Task RemoveAsync(string key, CancellationToken ct = default);
}
