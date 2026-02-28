using Microsoft.Extensions.Logging;

using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.Interfaces;
using WeeklyUp.Domain.Interfaces.Services;
using WeeklyUp.Infrastructure.Instagram;

namespace WeeklyUp.Infrastructure.BackgroundJobs;

public sealed class TokenRefreshJob
{
    private readonly IUnitOfWork _uow;
    private readonly ITokenEncryptor _encryptor;
    private readonly IInstagramOAuthService _instagramOAuth;
    private readonly ILogger<TokenRefreshJob> _logger;

    public TokenRefreshJob(
        IUnitOfWork uow,
        ITokenEncryptor encryptor,
        IInstagramOAuthService instagramOAuth,
        ILogger<TokenRefreshJob> logger)
    {
        _uow = uow;
        _encryptor = encryptor;
        _instagramOAuth = instagramOAuth;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var expired = await _uow.Integrations.GetActiveWithExpiredTokensAsync(ct);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "TokenRefreshJob: {Count} integracoes com tokens expirados encontradas",
                expired.Count);
        }

        foreach (var integration in expired)
        {
            await ProcessIntegrationAsync(integration, ct);
        }

        if (expired.Count > 0)
        {
            await _uow.SaveChangesAsync(ct);
        }
    }

    private async Task ProcessIntegrationAsync(Integration integration, CancellationToken ct)
    {
        if (integration.Provider == IntegrationProvider.Instagram)
        {
            await TryRefreshInstagramTokenAsync(integration, ct);
        }
        else
        {
            integration.MarkTokenExpired();
            _uow.Integrations.Update(integration);
        }
    }

    private async Task TryRefreshInstagramTokenAsync(
        Integration integration, CancellationToken ct)
    {
        try
        {
            string currentToken = _encryptor.Decrypt(integration.AccessToken);
            var refreshed = await _instagramOAuth.RefreshTokenAsync(currentToken, ct);

            if (refreshed.IsSuccess)
            {
                integration.UpdateTokens(refreshed.Value, refreshed.Value, _encryptor);
                _uow.Integrations.Update(integration);
                return;
            }

            _logger.LogWarning(
                "Falha ao renovar token Instagram para integracao {IntegrationId}: {Error}",
                integration.Id, refreshed.Error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex,
                "Excecao ao renovar token Instagram para integracao {IntegrationId}",
                integration.Id);
        }

        integration.MarkTokenExpired();
        _uow.Integrations.Update(integration);
    }
}
