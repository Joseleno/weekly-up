using Mediator;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Users.Commands.VerifyEmail;

public sealed record VerifyEmailCommand(Guid UserId, string Token) : ICommand<Result<bool>>;
