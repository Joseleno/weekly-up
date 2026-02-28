using Mediator;

using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Application.Common.Mappings;
using WeeklyUp.Domain.Interfaces.Repositories;
using WeeklyUp.Shared.Constants;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Users.Queries.GetUserDashboard;

public sealed class GetUserDashboardQueryHandler
    : IQueryHandler<GetUserDashboardQuery, Result<UserDashboardDto>>
{
    private static readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(30);

    private readonly IUserRepository _users;
    private readonly IReportRepository _reports;
    private readonly IManualMetricRepository _manualMetrics;
    private readonly IApplicationCacheService _cache;
    private readonly IDateTimeProvider _dateTime;

    public GetUserDashboardQueryHandler(
        IUserRepository users,
        IReportRepository reports,
        IManualMetricRepository manualMetrics,
        IApplicationCacheService cache,
        IDateTimeProvider dateTime)
    {
        _users = users;
        _reports = reports;
        _manualMetrics = manualMetrics;
        _cache = cache;
        _dateTime = dateTime;
    }

    public async ValueTask<Result<UserDashboardDto>> Handle(
        GetUserDashboardQuery query,
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.Dashboard(query.UserId);
        var cached = await _cache.GetAsync<UserDashboardDto>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var user = await _users.GetByIdWithIntegrationsAsync(query.UserId, cancellationToken);
        if (user is null)
        {
            return AppError.NotFound("User.NotFound", $"Usuário '{query.UserId}' não encontrado.");
        }

        var today = _dateTime.Today;
        var currentWeekStart = GetCurrentWeekMonday(today);

        var latestReport = await _reports.GetLatestAsync(query.UserId, cancellationToken);
        var metric = await _manualMetrics.GetByUserAndWeekAsync(
            query.UserId, currentWeekStart, cancellationToken);

        var dto = new UserDashboardDto(
            Profile: user.ToProfileDto(),
            LastReport: latestReport?.ToSummaryDto(),
            Integrations: user.Integrations.Select(i => i.ToDto()).ToList(),
            HasManualMetrics: metric is not null);

        await _cache.SetAsync(cacheKey, dto, _cacheExpiry, cancellationToken);
        return dto;
    }

    private static DateOnly GetCurrentWeekMonday(DateOnly date)
    {
        int daysSinceMonday = ((int)date.DayOfWeek - 1 + 7) % 7;
        return date.AddDays(-daysSinceMonday);
    }
}
