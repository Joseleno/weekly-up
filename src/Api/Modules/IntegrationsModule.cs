using Carter;

using Mediator;

using WeeklyUp.Api.Extensions;
using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Application.Integrations.Commands.ConnectIntegration;
using WeeklyUp.Application.Integrations.Commands.DisconnectIntegration;
using WeeklyUp.Application.Integrations.Queries.GetIntegrations;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Infrastructure.Instagram;
using WeeklyUp.Shared.Results;

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

        // Instagram OAuth — gera URL de autorização Meta
        group.MapGet("/instagram/auth", (
            ICurrentUserService currentUser,
            InstagramStateService stateService,
            IInstagramOAuthService oAuthService) =>
        {
            string state = stateService.GenerateState(currentUser.UserId);
            string url = oAuthService.BuildAuthUrl(state);
            return Results.Ok(new { url });
        })
        .Produces(StatusCodes.Status401Unauthorized);

        // Instagram OAuth callback — chamado pelo WebApp após redirect do Meta
        group.MapGet("/instagram/callback", async (
            string code,
            string state,
            ICurrentUserService currentUser,
            InstagramStateService stateService,
            IInstagramOAuthService oAuthService,
            CancellationToken ct) =>
        {
            var stateResult = stateService.ValidateState(state);
            if (stateResult.IsFailure)
            {
                return stateResult.Error.ToProblem();
            }

            if (stateResult.Value != currentUser.UserId)
            {
                return AppError.Forbidden("Instagram.StateMismatch", "State inválido.").ToProblem();
            }

            var result = await oAuthService.ExchangeCodeAsync(code, currentUser.UserId, ct);
            return result.Match(
                dto => Results.Ok(dto),
                error => error.ToProblem());
        })
        .Produces(StatusCodes.Status401Unauthorized);
    }
}
