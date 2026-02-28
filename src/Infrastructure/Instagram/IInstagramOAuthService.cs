using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Infrastructure.Instagram;

public interface IInstagramOAuthService
{
    public string BuildAuthUrl(string state);

    public Task<Result<IntegrationDto>> ExchangeCodeAsync(
        string code,
        Guid userId,
        CancellationToken ct = default);

    public Task<Result<string>> RefreshTokenAsync(
        string currentToken,
        CancellationToken ct = default);
}
