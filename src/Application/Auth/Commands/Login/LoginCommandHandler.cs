using Mediator;

using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Interfaces.Repositories;
using WeeklyUp.Domain.Interfaces.Services;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Auth.Commands.Login;

public sealed class LoginCommandHandler
    : ICommandHandler<LoginCommand, Result<AuthTokenDto>>
{
    private readonly IUserRepository _users;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IPasswordHasher _passwordHasher;

    public LoginCommandHandler(
        IUserRepository users,
        IJwtTokenGenerator jwtTokenGenerator,
        IDateTimeProvider dateTimeProvider,
        IPasswordHasher passwordHasher)
    {
        _users = users;
        _jwtTokenGenerator = jwtTokenGenerator;
        _dateTimeProvider = dateTimeProvider;
        _passwordHasher = passwordHasher;
    }

    public async ValueTask<Result<AuthTokenDto>> Handle(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        User? user = await _users.GetByEmailAsync(command.Email, cancellationToken);

        if (user is null || !user.IsActive || !user.VerifyPassword(command.Password, _passwordHasher))
        {
            return AppError.NotFound("Auth.InvalidCredentials", "Credenciais invalidas.");
        }

        string token = _jwtTokenGenerator.GenerateToken(user);
        var expiresAt = new DateTimeOffset(_dateTimeProvider.UtcNow).AddHours(1);
        return new AuthTokenDto(token, expiresAt);
    }
}
