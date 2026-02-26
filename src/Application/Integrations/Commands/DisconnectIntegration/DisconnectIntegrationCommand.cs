using Mediator;

using WeeklyUp.Domain.Enums;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Integrations.Commands.DisconnectIntegration;

public sealed record DisconnectIntegrationCommand(
    Guid UserId,
    IntegrationProvider Provider) : ICommand<Result<bool>>;
