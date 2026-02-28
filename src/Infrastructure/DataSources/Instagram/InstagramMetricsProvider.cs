using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.ValueObjects;
using WeeklyUp.Infrastructure.Instagram;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Infrastructure.DataSources.Instagram;

public sealed class InstagramMetricsProvider : IDataSourceProvider
{
    private static class InsightMetrics
    {
        public const string Reach = "reach";
        public const string Impressions = "impressions";
        public const string ProfileViews = "profile_views";
        public const string AccountFields = "id,username,followers_count,media_count";
        public const string MediaFields = "id,like_count,comments_count,timestamp";
        public const string Period = "week";
        public const int RecentMediaLimit = 12;
        public const string GraphBaseUrl = "https://graph.instagram.com/v21.0";
    }

    private static readonly JsonSerializerOptions _jsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;
    private readonly ILogger<InstagramMetricsProvider> _logger;

    public IntegrationProvider ProviderType => IntegrationProvider.Instagram;

    public InstagramMetricsProvider(
        IHttpClientFactory httpClientFactory,
        ILogger<InstagramMetricsProvider> logger)
    {
        _httpClient = httpClientFactory.CreateClient("instagram-graph");
        _logger = logger;
    }

    // propertyId = token de acesso de longa duração (decriptado pelo ConnectIntegration)
    // providerAccountId = Instagram User ID
    public async Task<Result<ReportMetrics>> GetMetricsAsync(
        Guid userId,
        string providerAccountId,
        string? propertyId,
        DateRange weekRange,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(propertyId))
        {
            return AppError.Failure("Instagram.NoToken", "Token de acesso Instagram ausente.");
        }

        try
        {
            var accountTask = FetchAccountAsync(propertyId, ct);
            var insightsTask = FetchInsightsAsync(propertyId, ct);
            var mediaTask = FetchRecentMediaAsync(propertyId, ct);

            await Task.WhenAll(accountTask, insightsTask, mediaTask);

            var account = await accountTask;
            var insights = await insightsTask;
            var media = await mediaTask;

            if (account is null || insights is null || media is null)
            {
                return AppError.Failure("Instagram.NoData", "Dados do Instagram indisponíveis.");
            }

            return BuildMetrics(account, insights, media);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Erro ao buscar metricas Instagram para usuario {UserId}", userId);
            return AppError.Failure("Instagram.Error", "Erro ao obter dados do Instagram.");
        }
    }

    public Task<Result<Demographics?>> GetDemographicsAsync(
        Guid userId,
        string providerAccountId,
        string? propertyId,
        DateRange weekRange,
        CancellationToken ct = default) =>
        Task.FromResult(Result.Success<Demographics?>(null));

    private async Task<InstagramAccountResponse?> FetchAccountAsync(
        string token, CancellationToken ct)
    {
        var url = $"{InsightMetrics.GraphBaseUrl}/me" +
                  $"?fields={InsightMetrics.AccountFields}" +
                  $"&access_token={Uri.EscapeDataString(token)}";
        var response = await _httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InstagramAccountResponse>(_jsonOptions, ct);
    }

    private async Task<InstagramInsightsResponse?> FetchInsightsAsync(
        string token, CancellationToken ct)
    {
        var metrics = string.Join(',', InsightMetrics.Reach, InsightMetrics.Impressions, InsightMetrics.ProfileViews);
        var url = $"{InsightMetrics.GraphBaseUrl}/me/insights" +
                  $"?metric={metrics}" +
                  $"&period={InsightMetrics.Period}" +
                  $"&access_token={Uri.EscapeDataString(token)}";
        var response = await _httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InstagramInsightsResponse>(_jsonOptions, ct);
    }

    private async Task<InstagramMediaListResponse?> FetchRecentMediaAsync(
        string token, CancellationToken ct)
    {
        var url = $"{InsightMetrics.GraphBaseUrl}/me/media" +
                  $"?fields={InsightMetrics.MediaFields}" +
                  $"&limit={InsightMetrics.RecentMediaLimit}" +
                  $"&access_token={Uri.EscapeDataString(token)}";
        var response = await _httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InstagramMediaListResponse>(_jsonOptions, ct);
    }

    private static ReportMetrics BuildMetrics(
        InstagramAccountResponse account,
        InstagramInsightsResponse insights,
        InstagramMediaListResponse media)
    {
        int reach = GetInsightTotal(insights, InsightMetrics.Reach);
        int impressions = GetInsightTotal(insights, InsightMetrics.Impressions);
        int profileViews = GetInsightTotal(insights, InsightMetrics.ProfileViews);
        int totalLikes = media.Data.Sum(m => m.LikeCount);
        int totalComments = media.Data.Sum(m => m.CommentsCount);

        return new ReportMetrics(
            revenue: Money.Zero,
            salesCount: totalLikes + totalComments,
            averageTicket: Money.Zero,
            newCustomers: account.FollowersCount,
            totalVisits: reach,
            uniqueVisitors: profileViews,
            pageViews: impressions,
            topPage: $"@{account.Username}",
            topTrafficSource: "Instagram",
            previousRevenue: null,
            previousVisits: null);
    }

    private static int GetInsightTotal(InstagramInsightsResponse insights, string name)
    {
        var item = insights.Data.FirstOrDefault(d => d.Name == name);
        return item?.Values.Sum(v => v.Value) ?? 0;
    }
}
