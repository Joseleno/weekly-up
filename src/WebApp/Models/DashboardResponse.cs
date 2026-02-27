namespace WeeklyUp.WebApp.Models;

public sealed record DashboardResponse(
    UserProfileResponse Profile,
    ReportSummaryResponse? LastReport,
    IReadOnlyList<IntegrationResponse> Integrations,
    bool HasManualMetrics);
