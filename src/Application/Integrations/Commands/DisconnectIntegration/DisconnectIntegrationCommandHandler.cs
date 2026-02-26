using Mediator;

using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Interfaces;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Integrations.Commands.DisconnectIntegration;

public sealed class DisconnectIntegrationCommandHandler
    : ICommandHandler<DisconnectIntegrationCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;

    public DisconnectIntegrationCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async ValueTask<Result<bool>> Handle(
        DisconnectIntegrationCommand command,
        CancellationToken cancellationToken)
    {
        User? user = await _uow.Users.GetByIdWithIntegrationsAsync(command.UserId, cancellationToken);

        if (user is null)
        {
            return AppError.NotFound("User.NotFound", $"Usuario '{command.UserId}' nao encontrado.");
        }

        Result<bool> result = user.RemoveIntegration(command.Provider);

        if (result.IsFailure)
        {
            return result.Error;
        }

        _uow.Users.Update(user);

        return true;
    }
}
