using Mediator;
using Microsoft.Extensions.Logging;
using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Application.Reports.Commands.GenerateWeeklyReport;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.Interfaces.Repositories;

namespace WeeklyUp.Infrastructure.BackgroundJobs;

public sealed class WeeklyReportGenerationJob
{
    private readonly IMediator _mediator;
    private readonly IUserRepository _users;
    private readonly IDateTimeProvider _dateTime;
    private readonly ILogger<WeeklyReportGenerationJob> _logger;

    public WeeklyReportGenerationJob(
        IMediator mediator,
        IUserRepository users,
        IDateTimeProvider dateTime,
        ILogger<WeeklyReportGenerationJob> logger)
    {
        _mediator = mediator;
        _users = users;
        _dateTime = dateTime;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var today = _dateTime.Today;
        var dayOfWeek = MapToDayOfWeekPreference(today.DayOfWeek);
        var users = await _users.GetActiveUsersForReportAsync(dayOfWeek, ct);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Gerando relatorios para {Count} usuarios no dia {Day}", users.Count, dayOfWeek);
        }

        foreach (var user in users)
        {
            var result = await _mediator.Send(new GenerateWeeklyReportCommand(user.Id), ct);
            if (!result.IsSuccess)
            {
                _logger.LogError("Falha ao gerar relatorio para usuario {UserId}: {Error}",
                    user.Id, result.Error);
            }
        }
    }

    private static DayOfWeekPreference MapToDayOfWeekPreference(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => DayOfWeekPreference.Monday,
        DayOfWeek.Tuesday => DayOfWeekPreference.Tuesday,
        DayOfWeek.Wednesday => DayOfWeekPreference.Wednesday,
        DayOfWeek.Thursday => DayOfWeekPreference.Thursday,
        DayOfWeek.Friday => DayOfWeekPreference.Friday,
        DayOfWeek.Saturday => DayOfWeekPreference.Saturday,
        _ => DayOfWeekPreference.Sunday,
    };
}
