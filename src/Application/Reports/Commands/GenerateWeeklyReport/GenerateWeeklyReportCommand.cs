using Mediator;

using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Reports.Commands.GenerateWeeklyReport;

public sealed record GenerateWeeklyReportCommand(Guid UserId) : ICommand<Result<bool>>;
