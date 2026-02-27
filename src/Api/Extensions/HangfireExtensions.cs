using Hangfire;
using WeeklyUp.Infrastructure.BackgroundJobs;

namespace WeeklyUp.Api.Extensions;

internal static class HangfireExtensions
{
    internal static IApplicationBuilder MapRecurringJobs(this IApplicationBuilder app)
    {
        IRecurringJobManager recurringJobs = app.ApplicationServices.GetRequiredService<IRecurringJobManager>();
        TimeZoneInfo brasiliaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");

        recurringJobs.AddOrUpdate<WeeklyReportGenerationJob>(
            recurringJobId: "weekly-report-generation",
            methodCall: j => j.ExecuteAsync(CancellationToken.None),
            cronExpression: "0 7 * * 1",
            options: new RecurringJobOptions { TimeZone = brasiliaTimeZone });

        recurringJobs.AddOrUpdate<TokenRefreshJob>(
            recurringJobId: "integration-token-refresh",
            methodCall: j => j.ExecuteAsync(CancellationToken.None),
            cronExpression: "0 */6 * * *");

        return app;
    }
}
