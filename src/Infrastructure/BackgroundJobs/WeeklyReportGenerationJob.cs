using Mediator;

using Microsoft.Extensions.Logging;

using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Application.Reports.Commands.GenerateWeeklyReport;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.Interfaces;

namespace WeeklyUp.Infrastructure.BackgroundJobs;

public sealed class WeeklyReportGenerationJob
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _dateTime;
    private readonly ILogger<WeeklyReportGenerationJob> _logger;

    public WeeklyReportGenerationJob(
        IMediator mediator,
        IUnitOfWork uow,
        IDateTimeProvider dateTime,
        ILogger<WeeklyReportGenerationJob> logger)
    {
        _mediator = mediator;
        _uow = uow;
        _dateTime = dateTime;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var today = _dateTime.Today;
        var dayOfWeek = MapToDayOfWeekPreference(today.DayOfWeek);
        var weekStart = GetMondayOfWeek(today);
        var users = await _uow.Users.GetActiveUsersForReportAsync(dayOfWeek, ct);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Gerando relatorios para {Count} usuarios no dia {Day}", users.Count, dayOfWeek);
        }

        foreach (var user in users)
        {
            var existing = await _uow.Reports.GetByUserAndWeekAsync(user.Id, weekStart, ct);
            if (existing is not null)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Relatorio ja existe para usuario {UserId} na semana {WeekStart}, pulando", user.Id, weekStart);
                }
                continue;
            }

            var result = await _mediator.Send(new GenerateWeeklyReportCommand(user.Id), ct);
            if (!result.IsSuccess)
            {
                _logger.LogError("Falha ao gerar relatorio para usuario {UserId}: {Error}",
                    user.Id, result.Error);
            }
        }
    }

    private static DateOnly GetMondayOfWeek(DateOnly date)
    {
        var daysFromMonday = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-daysFromMonday);
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
