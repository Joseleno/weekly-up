using Mediator;

using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Application.Common.Mappings;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Interfaces;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Users.Commands.RegisterUser;

public sealed class RegisterUserCommandHandler
    : ICommandHandler<RegisterUserCommand, Result<UserProfileDto>>
{
    private readonly IUnitOfWork _uow;

    public RegisterUserCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async ValueTask<Result<UserProfileDto>> Handle(
        RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        bool emailExists = await _uow.Users.GetByEmailAsync(command.Email, cancellationToken) is not null;
        if (emailExists)
        {
            return AppError.Conflict("User.EmailAlreadyExists", "Email ja esta em uso.");
        }

        Result<User> userResult = User.Create(
            command.Email,
            command.Name,
            command.BusinessName,
            command.BusinessType,
            command.ExternalAuthId);

        if (userResult.IsFailure)
        {
            return userResult.Error;
        }

        User user = userResult.Value;
        ReportPreference preference = ReportPreference.CreateDefault(user.Id);

        await _uow.Users.AddAsync(user, cancellationToken);
        await _uow.ReportPreferences.AddAsync(preference, cancellationToken);

        return user.ToProfileDto();
    }
}
