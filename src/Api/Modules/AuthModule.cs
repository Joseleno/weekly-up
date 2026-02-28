using Carter;

using Mediator;

using WeeklyUp.Api.Extensions;
using WeeklyUp.Application.Auth.Commands.Login;
using WeeklyUp.Application.Auth.Commands.VerifyEmailByToken;
using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Application.Users.Commands.RegisterUser;
using WeeklyUp.Domain.Enums;

namespace WeeklyUp.Api.Modules;

public sealed record LoginRequest(string Email, string Password);

public sealed record RegisterRequest(
    string Email,
    string Password,
    string Name,
    string BusinessName,
    BusinessType BusinessType);

public sealed record VerifyEmailTokenRequest(string Token);

public sealed class AuthModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/auth")
            .WithTags("Auth")
            .RequireRateLimiting("api");

        group.MapPost("/register", async (
            RegisterRequest request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new RegisterUserCommand(
                request.Email,
                request.Password,
                request.Name,
                request.BusinessName,
                request.BusinessType);
            var result = await mediator.Send(command, ct);
            return result.Match(
                dto => Results.Created("/api/users/profile", dto),
                error => error.ToProblem());
        });

        group.MapPost("/login", async (
            LoginRequest request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new LoginCommand(request.Email, request.Password);
            var result = await mediator.Send(command, ct);
            return result.Match(
                dto => Results.Ok(dto),
                error => error.ToProblem());
        });

        group.MapPost("/verify-email", async (
            VerifyEmailTokenRequest request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new VerifyEmailByTokenCommand(request.Token);
            var result = await mediator.Send(command, ct);
            return result.Match(
                _ => Results.NoContent(),
                error => error.ToProblem());
        });
    }
}
