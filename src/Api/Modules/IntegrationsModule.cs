using Carter;

using Mediator;

using WeeklyUp.Api.Extensions;
using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Application.Integrations.Commands.ConnectIntegration;
using WeeklyUp.Application.Integrations.Commands.DisconnectIntegration;
using WeeklyUp.Application.Integrations.Queries.GetIntegrations;
using WeeklyUp.Domain.Enums;

namespace WeeklyUp.Api.Modules;

public sealed record ConnectIntegrationRequest(
    IntegrationProvider Provider,
    string AccessToken,
    string RefreshToken,
    string AccountId,
    string? PropertyId);

public sealed class IntegrationsModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/integrations")
            .WithTags("Integrations")
            .RequireAuthorization()
            .RequireRateLimiting("api");

        group.MapGet("/", async (
            IMediator mediator,
            ICurrentUserService currentUser,
            CancellationToken ct) =>
        {
            var query = new GetIntegrationsQuery(currentUser.UserId);
            var result = await mediator.Send(query, ct);
            return result.Match(
                dto => Results.Ok(dto),
                error => error.ToProblem());
        })
        .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/", async (
            ConnectIntegrationRequest request,
            IMediator mediator,
            ICurrentUserService currentUser,
            CancellationToken ct) =>
        {
            var command = new ConnectIntegrationCommand(
                currentUser.UserId,
                request.Provider,
                request.AccessToken,
                request.RefreshToken,
                request.AccountId,
                request.PropertyId);
            var result = await mediator.Send(command, ct);
            return result.Match(
                dto => Results.Created($"/api/integrations", dto),
                error => error.ToProblem());
        })
        .Produces(StatusCodes.Status401Unauthorized);

        group.MapDelete("/{provider}", async (
            IntegrationProvider provider,
            IMediator mediator,
            ICurrentUserService currentUser,
            CancellationToken ct) =>
        {
            var command = new DisconnectIntegrationCommand(currentUser.UserId, provider);
            var result = await mediator.Send(command, ct);
            return result.Match(
                _ => Results.NoContent(),
                error => error.ToProblem());
        })
        .Produces(StatusCodes.Status401Unauthorized);
    }
}
