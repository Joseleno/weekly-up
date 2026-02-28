using Hangfire;

using WeeklyUp.Application.Common.Interfaces;

namespace WeeklyUp.Infrastructure.BackgroundJobs;

public sealed class HangfireReportJobScheduler : IReportJobScheduler
{
    private readonly IBackgroundJobClient _jobClient;

    public HangfireReportJobScheduler(IBackgroundJobClient jobClient)
    {
        _jobClient = jobClient;
    }

    public void ScheduleReportSending(Guid reportId)
    {
        _jobClient.Enqueue<ReportSendingJob>(
            j => j.ExecuteAsync(reportId, CancellationToken.None));
    }

    public void ScheduleDataGeneration(Guid reportId)
    {
        _jobClient.Enqueue<ReportDataGenerationJob>(
            j => j.ExecuteAsync(reportId, CancellationToken.None));
    }
}
