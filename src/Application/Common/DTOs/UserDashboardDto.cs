namespace WeeklyUp.Application.Common.DTOs;

public sealed record UserDashboardDto(
    UserProfileDto Profile,
    ReportSummaryDto? LastReport,
    IReadOnlyList<IntegrationDto> Integrations,
    bool HasManualMetrics);
