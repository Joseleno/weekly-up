using Mediator;

using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Integrations.Queries.GetIntegrations;

public sealed record GetIntegrationsQuery(Guid UserId) : IQuery<Result<IReadOnlyList<IntegrationDto>>>;
