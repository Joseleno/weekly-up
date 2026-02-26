using Mediator;

using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Auth.Commands.Login;

public sealed record LoginCommand(
    string ExternalAuthId,
    string Email) : ICommand<Result<AuthTokenDto>>;
