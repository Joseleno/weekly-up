using Mediator;

using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Reports.Queries.GetReportPreferences;

public sealed record GetReportPreferencesQuery(Guid UserId)
    : IQuery<Result<ReportPreferencesDto>>;
