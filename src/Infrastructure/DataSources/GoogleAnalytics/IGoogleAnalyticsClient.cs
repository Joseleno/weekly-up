using Refit;

namespace WeeklyUp.Infrastructure.DataSources.GoogleAnalytics;

[Headers("Content-Type: application/json")]
public interface IGoogleAnalyticsClient
{
    [Post("/v1beta/properties/{propertyId}:runReport")]
    public Task<GA4ReportResponse> RunReportAsync(
        [AliasAs("propertyId")] string propertyId,
        [Body] GA4ReportRequest request,
        CancellationToken ct = default);
}
