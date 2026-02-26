using Mediator;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Reports.Commands.UpdateReportPreferences;

public sealed record UpdateReportPreferencesCommand(
    Guid UserId,
    DayOfWeekPreference SendDay,
    TimeOnly SendTime,
    IReadOnlyList<string> EnabledSections) : ICommand<Result<bool>>;
