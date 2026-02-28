using Mediator;

using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Domain.Interfaces.Repositories;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Reports.Queries.GetReportPreferences;

public sealed class GetReportPreferencesQueryHandler
    : IQueryHandler<GetReportPreferencesQuery, Result<ReportPreferencesDto>>
{
    private static readonly ReportPreferencesDto _default =
        new("Monday", "07:00:00", []);

    private readonly IReportPreferenceRepository _prefs;

    public GetReportPreferencesQueryHandler(IReportPreferenceRepository prefs)
        => _prefs = prefs;

    public async ValueTask<Result<ReportPreferencesDto>> Handle(
        GetReportPreferencesQuery query,
        CancellationToken cancellationToken)
    {
        var pref = await _prefs.GetByUserIdAsync(query.UserId, cancellationToken);

        if (pref is null)
        {
            return _default;
        }

        return new ReportPreferencesDto(
            SendDay: pref.SendDay.ToString(),
            SendTime: pref.SendTime.ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
            EnabledSections: pref.EnabledSections);
    }
}
