using Riok.Mapperly.Abstractions;
using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.ValueObjects;

namespace WeeklyUp.Application.Common.Mappings;

[Mapper]
public static partial class ReportMapper
{
    public static ReportSummaryDto ToSummaryDto(this Report report) =>
        new(
            Id: report.Id,
            WeekLabel: report.WeekRange.ToString(),
            Status: report.Status.ToString(),
            Revenue: report.Metrics?.Revenue.Amount,
            SalesCount: report.Metrics?.SalesCount,
            CreatedAt: new DateTimeOffset(report.CreatedAt, TimeSpan.Zero));

    public static ReportDetailDto ToDetailDto(this Report report) =>
        new(
            Id: report.Id,
            WeekLabel: report.WeekRange.ToString(),
            Status: report.Status.ToString(),
            Metrics: report.Metrics is null ? null : MapMetrics(report.Metrics),
            Insights: report.Insights is null ? null : MapInsights(report.Insights),
            Demographics: report.Demographics is null ? null : MapDemographics(report.Demographics),
            CreatedAt: new DateTimeOffset(report.CreatedAt, TimeSpan.Zero));

    private static MetricsDto MapMetrics(ReportMetrics metrics) =>
        new(
            Revenue: metrics.Revenue.Amount,
            SalesCount: metrics.SalesCount,
            AverageTicket: metrics.AverageTicket.Amount,
            NewCustomers: metrics.NewCustomers,
            Visits: metrics.TotalVisits,
            PageViews: metrics.PageViews,
            TopPage: metrics.TopPage,
            TopSource: metrics.TopTrafficSource);

    private static InsightsDto MapInsights(ReportInsights insights) =>
        new(
            Highlight: insights.Highlight,
            Alert: insights.Alert,
            Tip: insights.Tip,
            GeneratedAt: new DateTimeOffset(insights.GeneratedAt, TimeSpan.Zero));

    private static DemographicsDto MapDemographics(Demographics demographics) =>
        new(
            AgeGroups: demographics.AgeGroups
                .Select(kvp => new AgeGroupDto(Range: kvp.Key, Percentage: kvp.Value))
                .ToList(),
            TopCities: demographics.TopCities.ToList());
}
