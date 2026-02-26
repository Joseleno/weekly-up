using Mediator;

using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Users.Queries.GetUserDashboard;

public sealed record GetUserDashboardQuery(Guid UserId) : IQuery<Result<UserDashboardDto>>;
