using Mediator;

using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Interfaces;
using WeeklyUp.Domain.Interfaces.Services;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Users.Commands.RegisterUser;

public sealed class RegisterUserCommandHandler
    : ICommandHandler<RegisterUserCommand, Result<AuthTokenDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RegisterUserCommandHandler(
        IUnitOfWork uow,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IDateTimeProvider dateTimeProvider)
    {
        _uow = uow;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _dateTimeProvider = dateTimeProvider;
    }

    public async ValueTask<Result<AuthTokenDto>> Handle(
        RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        bool emailExists = await _uow.Users.GetByEmailAsync(command.Email, cancellationToken) is not null;
        if (emailExists)
        {
            return AppError.Conflict("User.EmailAlreadyExists", "Email ja esta em uso.");
        }

        string verificationToken = Guid.NewGuid().ToString("N");
        string passwordHash = _passwordHasher.Hash(command.Password);

        Result<User> userResult = User.Create(
            command.Email,
            command.Name,
            command.BusinessName,
            command.BusinessType,
            externalAuthId: null,
            verificationToken: verificationToken,
            passwordHash: passwordHash);

        if (userResult.IsFailure)
        {
            return userResult.Error;
        }

        User user = userResult.Value;
        ReportPreference preference = ReportPreference.CreateDefault(user.Id);

        await _uow.Users.AddAsync(user, cancellationToken);
        await _uow.ReportPreferences.AddAsync(preference, cancellationToken);

        string token = _jwtTokenGenerator.GenerateToken(user);
        var expiresAt = new DateTimeOffset(_dateTimeProvider.UtcNow).AddHours(1);
        return new AuthTokenDto(token, expiresAt);
    }
}
