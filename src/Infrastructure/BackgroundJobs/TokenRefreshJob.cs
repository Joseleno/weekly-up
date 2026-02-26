using Microsoft.Extensions.Logging;
using WeeklyUp.Domain.Interfaces;

namespace WeeklyUp.Infrastructure.BackgroundJobs;

public sealed class TokenRefreshJob
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<TokenRefreshJob> _logger;

    public TokenRefreshJob(
        IUnitOfWork uow,
        ILogger<TokenRefreshJob> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var expired = await _uow.Integrations.GetActiveWithExpiredTokensAsync(ct);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("TokenRefreshJob: {Count} integracoes com tokens expirados encontradas", expired.Count);
        }

        foreach (var integration in expired)
        {
            integration.MarkTokenExpired();
            _uow.Integrations.Update(integration);
        }

        if (expired.Count > 0)
        {
            await _uow.SaveChangesAsync(ct);
        }
    }
}
