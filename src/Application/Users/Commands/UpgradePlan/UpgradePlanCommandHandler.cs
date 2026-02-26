using Mediator;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Interfaces;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Users.Commands.UpgradePlan;

public sealed class UpgradePlanCommandHandler
    : ICommandHandler<UpgradePlanCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;

    public UpgradePlanCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async ValueTask<Result<bool>> Handle(
        UpgradePlanCommand command,
        CancellationToken cancellationToken)
    {
        User? user = await _uow.Users.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return AppError.NotFound("User.NotFound", $"Usuario com Id '{command.UserId}' nao encontrado.");
        }

        Result<bool> upgradeResult = user.UpgradePlan(command.NewPlan);
        if (upgradeResult.IsFailure)
        {
            return upgradeResult.Error;
        }

        _uow.Users.Update(user);

        return true;
    }
}
