using Mediator;

using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Interfaces.Repositories;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Auth.Commands.Login;

public sealed class LoginCommandHandler
    : ICommandHandler<LoginCommand, Result<AuthTokenDto>>
{
    private readonly IUserRepository _users;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;

    public LoginCommandHandler(
        IUserRepository users,
        IJwtTokenGenerator jwtTokenGenerator,
        IDateTimeProvider dateTimeProvider)
    {
        _users = users;
        _jwtTokenGenerator = jwtTokenGenerator;
        _dateTimeProvider = dateTimeProvider;
    }

    public async ValueTask<Result<AuthTokenDto>> Handle(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        User? user = await _users.GetByExternalAuthIdAsync(command.ExternalAuthId, cancellationToken)
            ?? await _users.GetByEmailAsync(command.Email, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return AppError.NotFound("Auth.UserNotFound", "Credenciais invalidas ou usuario nao encontrado.");
        }

        string token = _jwtTokenGenerator.GenerateToken(user);

        var expiresAt = new DateTimeOffset(_dateTimeProvider.UtcNow).AddHours(1);
        return new AuthTokenDto(token, expiresAt);
    }
}
