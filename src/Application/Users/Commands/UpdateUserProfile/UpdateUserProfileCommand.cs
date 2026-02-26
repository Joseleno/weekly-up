using Mediator;

using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Users.Commands.UpdateUserProfile;

public sealed record UpdateUserProfileCommand(
    Guid UserId,
    string Name,
    string BusinessName,
    BusinessType BusinessType) : ICommand<Result<UserProfileDto>>;
