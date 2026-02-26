using Mediator;

using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Interfaces;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Users.Commands.VerifyEmail;

public sealed class VerifyEmailCommandHandler
    : ICommandHandler<VerifyEmailCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;

    public VerifyEmailCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async ValueTask<Result<bool>> Handle(
        VerifyEmailCommand command,
        CancellationToken cancellationToken)
    {
        User? user = await _uow.Users.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return AppError.NotFound("User.NotFound", $"Usuario com Id '{command.UserId}' nao encontrado.");
        }

        user.VerifyEmail();
        _uow.Users.Update(user);

        return true;
    }
}
