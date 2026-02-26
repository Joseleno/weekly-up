namespace WeeklyUp.Shared.Constants;

public static class CacheKeys
{
    public const string DashboardPrefix = "dashboard:";
    public const string UserProfilePrefix = "user-profile:";
    public const string IntegrationsPrefix = "integrations:";
    public const string ReportPrefix = "report:";

    public static string Dashboard(Guid userId) => $"{DashboardPrefix}{userId}";
    public static string UserProfile(Guid userId) => $"{UserProfilePrefix}{userId}";
    public static string Integrations(Guid userId) => $"{IntegrationsPrefix}{userId}";
    public static string Report(Guid reportId) => $"{ReportPrefix}{reportId}";
}
