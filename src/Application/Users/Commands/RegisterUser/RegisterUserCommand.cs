using Mediator;

using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Users.Commands.RegisterUser;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string Name,
    string BusinessName,
    BusinessType BusinessType) : ICommand<Result<AuthTokenDto>>;
