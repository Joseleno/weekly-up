using System.Text.Json.Serialization;

namespace WeeklyUp.Infrastructure.DataSources.GoogleAnalytics;

public sealed record GA4DateRange(
    [property: JsonPropertyName("startDate")] string StartDate,
    [property: JsonPropertyName("endDate")] string EndDate);

public sealed record GA4Metric([property: JsonPropertyName("name")] string Name);

public sealed record GA4Dimension([property: JsonPropertyName("name")] string Name);

public sealed record GA4ReportRequest(
    [property: JsonPropertyName("dateRanges")] IReadOnlyList<GA4DateRange> DateRanges,
    [property: JsonPropertyName("metrics")] IReadOnlyList<GA4Metric> Metrics,
    [property: JsonPropertyName("dimensions")] IReadOnlyList<GA4Dimension> Dimensions);

public sealed record GA4MetricValue([property: JsonPropertyName("value")] string Value);

public sealed record GA4DimensionValue([property: JsonPropertyName("value")] string Value);

public sealed record GA4Row(
    [property: JsonPropertyName("metricValues")] IReadOnlyList<GA4MetricValue> MetricValues,
    [property: JsonPropertyName("dimensionValues")] IReadOnlyList<GA4DimensionValue> DimensionValues);

public sealed record GA4ReportResponse(
    [property: JsonPropertyName("rows")] IReadOnlyList<GA4Row>? Rows);
