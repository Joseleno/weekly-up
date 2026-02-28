using System.Net.Http.Json;
using System.Text.Json;

using Mediator;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Application.Common.Mappings;
using WeeklyUp.Application.Integrations.Commands.ConnectIntegration;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Infrastructure.Instagram;

public sealed class InstagramOAuthService : IInstagramOAuthService
{
    private static class MetaUrls
    {
        public const string OAuthAuthorize = "https://api.instagram.com/oauth/authorize";
        public const string TokenExchange = "https://api.instagram.com/oauth/access_token";
        public const string LongLivedToken = "https://graph.instagram.com/access_token";
        public const string AccountInfo = "https://graph.instagram.com/v21.0/me";
    }

    private static class OAuthScopes
    {
        public const string Required = "instagram_basic,instagram_manage_insights";
    }

    private static readonly JsonSerializerOptions _jsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;
    private readonly InstagramOptions _opts;
    private readonly IMediator _mediator;
    private readonly ILogger<InstagramOAuthService> _logger;

    public InstagramOAuthService(
        IHttpClientFactory httpClientFactory,
        IOptions<InstagramOptions> opts,
        IMediator mediator,
        ILogger<InstagramOAuthService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("instagram-oauth");
        _opts = opts.Value;
        _mediator = mediator;
        _logger = logger;
    }

    public string BuildAuthUrl(string state)
    {
        var qs = System.Web.HttpUtility.ParseQueryString(string.Empty);
        qs["client_id"] = _opts.ClientId;
        qs["redirect_uri"] = _opts.RedirectUri;
        qs["scope"] = OAuthScopes.Required;
        qs["response_type"] = "code";
        qs["state"] = state;
        return $"{MetaUrls.OAuthAuthorize}?{qs}";
    }

    public async Task<Result<IntegrationDto>> ExchangeCodeAsync(
        string code,
        Guid userId,
        CancellationToken ct = default)
    {
        var shortLivedResult = await ExchangeForShortLivedTokenAsync(code, ct);
        if (shortLivedResult.IsFailure)
        {
            return shortLivedResult.Error;
        }

        var longLivedResult = await ExchangeForLongLivedTokenAsync(shortLivedResult.Value, ct);
        if (longLivedResult.IsFailure)
        {
            return longLivedResult.Error;
        }

        string longLivedToken = longLivedResult.Value;

        var accountResult = await GetAccountInfoAsync(longLivedToken, ct);
        if (accountResult.IsFailure)
        {
            return accountResult.Error;
        }

        InstagramAccountResponse account = accountResult.Value;

        var command = new ConnectIntegrationCommand(
            userId,
            IntegrationProvider.Instagram,
            longLivedToken,
            longLivedToken,
            account.Id,
            longLivedToken);

        return await _mediator.Send(command, ct);
    }

    public async Task<Result<string>> RefreshTokenAsync(
        string currentToken,
        CancellationToken ct = default)
    {
        var url = $"https://graph.instagram.com/refresh_access_token" +
                  $"?grant_type=ig_refresh_token" +
                  $"&access_token={Uri.EscapeDataString(currentToken)}";

        var response = await _httpClient.GetAsync(url, ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Falha ao renovar token Instagram. Status: {Status}", response.StatusCode);
            return AppError.Failure("Instagram.RefreshFailed", "Falha ao renovar token.");
        }

        var token = await response.Content
            .ReadFromJsonAsync<InstagramTokenRefreshResponse>(_jsonOptions, ct);

        if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
        {
            return AppError.Failure("Instagram.RefreshFailed", "Resposta invalida ao renovar token.");
        }

        return token.AccessToken;
    }

    private async Task<Result<string>> ExchangeForShortLivedTokenAsync(
        string code, CancellationToken ct)
    {
        var formData = new Dictionary<string, string>
        {
            ["client_id"] = _opts.ClientId,
            ["client_secret"] = _opts.ClientSecret,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = _opts.RedirectUri,
            ["code"] = code,
        };

        using var content = new FormUrlEncodedContent(formData);
        var response = await _httpClient.PostAsync(MetaUrls.TokenExchange, content, ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Falha na troca do codigo Instagram. Status: {Status}",
                response.StatusCode);
            return AppError.Failure("Instagram.OAuthFailed", "Falha na autorizacao do Instagram.");
        }

        var token = await response.Content
            .ReadFromJsonAsync<InstagramShortLivedTokenResponse>(_jsonOptions, ct);

        if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
        {
            return AppError.Failure("Instagram.OAuthFailed", "Resposta invalida do Instagram.");
        }

        return token.AccessToken;
    }

    private async Task<Result<string>> ExchangeForLongLivedTokenAsync(
        string shortLivedToken, CancellationToken ct)
    {
        var url = $"{MetaUrls.LongLivedToken}?grant_type=ig_exchange_token" +
                  $"&client_id={Uri.EscapeDataString(_opts.ClientId)}" +
                  $"&client_secret={Uri.EscapeDataString(_opts.ClientSecret)}" +
                  $"&access_token={Uri.EscapeDataString(shortLivedToken)}";

        var response = await _httpClient.GetAsync(url, ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Falha ao obter token de longa duracao. Status: {Status}",
                response.StatusCode);
            return AppError.Failure("Instagram.OAuthFailed", "Falha ao obter token Instagram.");
        }

        var token = await response.Content
            .ReadFromJsonAsync<InstagramLongLivedTokenResponse>(_jsonOptions, ct);

        if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
        {
            return AppError.Failure("Instagram.OAuthFailed", "Resposta invalida do Instagram.");
        }

        return token.AccessToken;
    }

    private async Task<Result<InstagramAccountResponse>> GetAccountInfoAsync(
        string accessToken, CancellationToken ct)
    {
        var url = $"{MetaUrls.AccountInfo}?fields=id,username,followers_count,media_count" +
                  $"&access_token={Uri.EscapeDataString(accessToken)}";

        var response = await _httpClient.GetAsync(url, ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Falha ao obter info da conta Instagram. Status: {Status}",
                response.StatusCode);
            return AppError.Failure("Instagram.AccountError", "Falha ao obter dados da conta.");
        }

        var account = await response.Content
            .ReadFromJsonAsync<InstagramAccountResponse>(_jsonOptions, ct);

        if (account is null || string.IsNullOrWhiteSpace(account.Id))
        {
            return AppError.Failure("Instagram.AccountError", "Dados da conta invalidos.");
        }

        return account;
    }
}
