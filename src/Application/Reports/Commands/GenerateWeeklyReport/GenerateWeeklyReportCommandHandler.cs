using Mediator;

using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Interfaces;
using WeeklyUp.Domain.ValueObjects;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Reports.Commands.GenerateWeeklyReport;

public sealed class GenerateWeeklyReportCommandHandler
    : ICommandHandler<GenerateWeeklyReportCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;
    private readonly IReportJobScheduler _jobScheduler;
    private readonly IDateTimeProvider _dateTime;

    public GenerateWeeklyReportCommandHandler(
        IUnitOfWork uow,
        IReportJobScheduler jobScheduler,
        IDateTimeProvider dateTime)
    {
        _uow = uow;
        _jobScheduler = jobScheduler;
        _dateTime = dateTime;
    }

    public async ValueTask<Result<bool>> Handle(
        GenerateWeeklyReportCommand command,
        CancellationToken cancellationToken)
    {
        var today = _dateTime.Today;
        var daysToMonday = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        var weekStart = today.AddDays(-daysToMonday);
        var weekEnd = weekStart.AddDays(6);

        var existing = await _uow.Reports.GetByUserAndWeekAsync(
            command.UserId, weekStart, cancellationToken);

        if (existing is not null)
        {
            return AppError.Conflict(
                "Report.AlreadyExists",
                "Já existe um relatório para esta semana.");
        }

        var weekRangeResult = DateRange.Create(weekStart, weekEnd);
        if (weekRangeResult.IsFailure)
        {
            return weekRangeResult.Error;
        }

        var report = Report.Create(command.UserId, weekRangeResult.Value);
        await _uow.Reports.AddAsync(report, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        _jobScheduler.ScheduleReportSending(report.Id);

        return true;
    }
}
