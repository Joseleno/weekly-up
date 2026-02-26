using Mediator;
using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Application.Common.Mappings;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Interfaces;
using WeeklyUp.Domain.Interfaces.Services;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Integrations.Commands.ConnectIntegration;

public sealed class ConnectIntegrationCommandHandler
    : ICommandHandler<ConnectIntegrationCommand, Result<IntegrationDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly ITokenEncryptor _encryptor;

    public ConnectIntegrationCommandHandler(IUnitOfWork uow, ITokenEncryptor encryptor)
    {
        _uow = uow;
        _encryptor = encryptor;
    }

    public async ValueTask<Result<IntegrationDto>> Handle(
        ConnectIntegrationCommand command,
        CancellationToken cancellationToken)
    {
        User? user = await _uow.Users.GetByIdWithIntegrationsAsync(command.UserId, cancellationToken);

        if (user is null)
        {
            return AppError.NotFound("User.NotFound", $"Usuario '{command.UserId}' nao encontrado.");
        }

        Result<Integration> result = user.AddIntegration(
            command.Provider,
            command.AccessToken,
            command.RefreshToken,
            command.AccountId,
            command.PropertyId,
            _encryptor);

        if (result.IsFailure)
        {
            return result.Error;
        }

        _uow.Users.Update(user);

        return result.Value.ToDto();
    }
}
