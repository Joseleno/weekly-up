using Mediator;

using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Application.Common.Mappings;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Interfaces;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Users.Commands.UpdateUserProfile;

public sealed class UpdateUserProfileCommandHandler
    : ICommandHandler<UpdateUserProfileCommand, Result<UserProfileDto>>
{
    private readonly IUnitOfWork _uow;

    public UpdateUserProfileCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async ValueTask<Result<UserProfileDto>> Handle(
        UpdateUserProfileCommand command,
        CancellationToken cancellationToken)
    {
        User? user = await _uow.Users.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return AppError.NotFound("User.NotFound", $"Usuario com Id '{command.UserId}' nao encontrado.");
        }

        Result<bool> updateResult = user.UpdateProfile(command.Name, command.BusinessName, command.BusinessType);
        if (updateResult.IsFailure)
        {
            return updateResult.Error;
        }

        _uow.Users.Update(user);

        return user.ToProfileDto();
    }
}
