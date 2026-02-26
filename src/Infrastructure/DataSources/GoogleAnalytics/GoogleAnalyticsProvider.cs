using System.Globalization;
using Microsoft.Extensions.Logging;
using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.ValueObjects;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Infrastructure.DataSources.GoogleAnalytics;

public sealed class GoogleAnalyticsProvider : IDataSourceProvider
{
    private static class MetricIndex
    {
        public const int TotalRevenue = 0;
        public const int Transactions = 1;
        public const int NewUsers = 2;
        public const int Sessions = 3;
        public const int ActiveUsers = 4;
        public const int ScreenPageViews = 5;
    }

    private static class DimensionIndex
    {
        public const int PagePath = 0;
        public const int ChannelGroup = 1;
    }

    private readonly IGoogleAnalyticsClient _client;
    private readonly ILogger<GoogleAnalyticsProvider> _logger;

    public IntegrationProvider ProviderType => IntegrationProvider.GoogleAnalytics4;

    public GoogleAnalyticsProvider(IGoogleAnalyticsClient client, ILogger<GoogleAnalyticsProvider> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<Result<ReportMetrics>> GetMetricsAsync(
        Guid userId, string providerAccountId, string? propertyId, DateRange weekRange, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(propertyId))
        {
            return AppError.Validation("GoogleAnalytics.NoPropertyId", "PropertyId e obrigatorio para Google Analytics.");
        }

        try
        {
            var request = BuildMetricsRequest(weekRange);
            GA4ReportResponse response = await _client.RunReportAsync(propertyId, request, ct);
            return MapToReportMetrics(response);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro ao buscar metricas do Google Analytics para usuario {UserId}", userId);
            return AppError.Failure("GoogleAnalytics.Error", ex.Message);
        }
    }

    public async Task<Result<Demographics?>> GetDemographicsAsync(
        Guid userId, string providerAccountId, string? propertyId, DateRange weekRange, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(propertyId))
        {
            return (Demographics?)null;
        }

        try
        {
            var request = BuildDemographicsRequest(weekRange);
            GA4ReportResponse response = await _client.RunReportAsync(propertyId, request, ct);
            return MapToDemographics(response);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Erro ao buscar demographics do GA para usuario {UserId}", userId);
            return (Demographics?)null;
        }
    }

    private static GA4ReportRequest BuildMetricsRequest(DateRange weekRange) =>
        new(
            DateRanges: [new(weekRange.Start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), weekRange.End.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))],
            Metrics: [new("totalRevenue"), new("transactions"), new("newUsers"), new("sessions"), new("activeUsers"), new("screenPageViews")],
            Dimensions: [new("pagePath"), new("sessionDefaultChannelGroup")]);

    private static GA4ReportRequest BuildDemographicsRequest(DateRange weekRange) =>
        new(
            DateRanges: [new(weekRange.Start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), weekRange.End.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))],
            Metrics: [new("sessions")],
            Dimensions: [new("userGender"), new("userAgeBracket"), new("city"), new("region"), new("deviceCategory")]);

    private static ReportMetrics MapToReportMetrics(GA4ReportResponse response)
    {
        GA4Row? row = response.Rows is { Count: > 0 } rows ? rows[0] : null;
        decimal revenue = ParseDecimal(row, MetricIndex.TotalRevenue);
        int transactions = ParseInt(row, MetricIndex.Transactions);
        decimal avgTicket = transactions > 0 ? revenue / transactions : 0m;

        return new ReportMetrics(
            revenue: Money.BRL(revenue),
            salesCount: transactions,
            averageTicket: Money.BRL(avgTicket),
            newCustomers: ParseInt(row, MetricIndex.NewUsers),
            totalVisits: ParseInt(row, MetricIndex.Sessions),
            uniqueVisitors: ParseInt(row, MetricIndex.ActiveUsers),
            pageViews: ParseInt(row, MetricIndex.ScreenPageViews),
            topPage: row?.DimensionValues?.ElementAtOrDefault(DimensionIndex.PagePath)?.Value,
            topTrafficSource: row?.DimensionValues?.ElementAtOrDefault(DimensionIndex.ChannelGroup)?.Value,
            previousRevenue: null,
            previousVisits: null);
    }

    private Demographics MapToDemographics(GA4ReportResponse response)
    {
        // TODO: Implementar mapeamento de demographics a partir da resposta GA4 — Issue #demographics
        _logger.LogWarning("MapToDemographics nao implementado — retornando objeto vazio para propertyId associado a resposta recebida.");
        return new Demographics(
            genderDistribution: new Dictionary<string, decimal>(),
            ageGroups: new Dictionary<string, decimal>(),
            topCities: [],
            topStates: [],
            devices: new Dictionary<string, decimal>(),
            trafficSources: new Dictionary<string, decimal>());
    }

    private static decimal ParseDecimal(GA4Row? row, int index) =>
        decimal.TryParse(row?.MetricValues?.ElementAtOrDefault(index)?.Value, out decimal v) ? v : 0m;

    private static int ParseInt(GA4Row? row, int index) =>
        int.TryParse(row?.MetricValues?.ElementAtOrDefault(index)?.Value, out int v) ? v : 0;
}
