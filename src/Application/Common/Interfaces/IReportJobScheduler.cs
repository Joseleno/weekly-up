namespace WeeklyUp.Application.Common.Interfaces;

public interface IReportJobScheduler
{
    public void ScheduleReportSending(Guid reportId);
    public void ScheduleDataGeneration(Guid reportId);
}
