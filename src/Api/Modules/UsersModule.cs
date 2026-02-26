using Carter;

using Mediator;

using WeeklyUp.Api.Extensions;
using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Application.Users.Commands.RegisterUser;
using WeeklyUp.Application.Users.Commands.UpdateUserProfile;
using WeeklyUp.Application.Users.Commands.UpgradePlan;
using WeeklyUp.Application.Users.Commands.VerifyEmail;
using WeeklyUp.Application.Users.Queries.GetUserDashboard;
using WeeklyUp.Application.Users.Queries.GetUserProfile;
using WeeklyUp.Domain.Enums;

namespace WeeklyUp.Api.Modules;

public sealed record RegisterUserRequest(
    string Email,
    string Name,
    string BusinessName,
    BusinessType BusinessType,
    string? ExternalAuthId);

public sealed record VerifyEmailRequest(Guid UserId, string Token);

public sealed record UpdateProfileRequest(
    string Name,
    string BusinessName,
    BusinessType BusinessType);

public sealed record UpgradePlanRequest(PlanType NewPlan);

public sealed class UsersModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/users")
            .WithTags("Users");

        group.MapPost("/", async (
            RegisterUserRequest request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new RegisterUserCommand(
                request.Email,
                request.Name,
                request.BusinessName,
                request.BusinessType,
                request.ExternalAuthId);
            var result = await mediator.Send(command, ct);
            return result.Match(
                dto => Results.Created($"/api/users/{dto.Id}", dto),
                error => error.ToProblem());
        });

        group.MapPost("/verify", async (
            VerifyEmailRequest request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new VerifyEmailCommand(request.UserId, request.Token);
            var result = await mediator.Send(command, ct);
            return result.Match(
                _ => Results.NoContent(),
                error => error.ToProblem());
        });

        group.MapPut("/profile", async (
            UpdateProfileRequest request,
            IMediator mediator,
            ICurrentUserService currentUser,
            CancellationToken ct) =>
        {
            var command = new UpdateUserProfileCommand(
                currentUser.UserId,
                request.Name,
                request.BusinessName,
                request.BusinessType);
            var result = await mediator.Send(command, ct);
            return result.Match(
                dto => Results.Ok(dto),
                error => error.ToProblem());
        }).RequireAuthorization();

        group.MapPut("/plan", async (
            UpgradePlanRequest request,
            IMediator mediator,
            ICurrentUserService currentUser,
            CancellationToken ct) =>
        {
            var command = new UpgradePlanCommand(currentUser.UserId, request.NewPlan);
            var result = await mediator.Send(command, ct);
            return result.Match(
                _ => Results.NoContent(),
                error => error.ToProblem());
        }).RequireAuthorization();

        group.MapGet("/me", async (
            IMediator mediator,
            ICurrentUserService currentUser,
            CancellationToken ct) =>
        {
            var query = new GetUserProfileQuery(currentUser.UserId);
            var result = await mediator.Send(query, ct);
            return result.Match(
                dto => Results.Ok(dto),
                error => error.ToProblem());
        }).RequireAuthorization();

        group.MapGet("/dashboard", async (
            IMediator mediator,
            ICurrentUserService currentUser,
            CancellationToken ct) =>
        {
            var query = new GetUserDashboardQuery(currentUser.UserId);
            var result = await mediator.Send(query, ct);
            return result.Match(
                dto => Results.Ok(dto),
                error => error.ToProblem());
        }).RequireAuthorization();
    }
}
