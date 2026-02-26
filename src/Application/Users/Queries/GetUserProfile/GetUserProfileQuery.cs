using Mediator;

using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Users.Queries.GetUserProfile;

public sealed record GetUserProfileQuery(Guid UserId) : IQuery<Result<UserProfileDto>>;
