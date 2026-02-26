using Mediator;

using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Integrations.Commands.ConnectIntegration;

public sealed record ConnectIntegrationCommand(
    Guid UserId,
    IntegrationProvider Provider,
    string AccessToken,
    string RefreshToken,
    string AccountId,
    string? PropertyId) : ICommand<Result<IntegrationDto>>;
