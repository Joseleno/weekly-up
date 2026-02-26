using Carter;
using Mediator;
using WeeklyUp.Api.Extensions;
using WeeklyUp.Application.Auth.Commands.Login;

namespace WeeklyUp.Api.Modules;

public sealed record LoginRequest(string ExternalAuthId, string Email);

public sealed class AuthModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/auth")
            .WithTags("Auth");

        group.MapPost("/login", async (
            LoginRequest request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new LoginCommand(request.ExternalAuthId, request.Email);
            var result = await mediator.Send(command, ct);
            return result.Match(
                dto => Results.Ok(dto),
                error => error.ToProblem());
        });
    }
}
