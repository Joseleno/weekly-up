using Mediator;

using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Auth.Commands.VerifyEmailByToken;

public sealed record VerifyEmailByTokenCommand(string Token) : ICommand<Result<bool>>;
