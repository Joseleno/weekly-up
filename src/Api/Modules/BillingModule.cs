using Carter;

using Mediator;

using WeeklyUp.Api.Extensions;
using WeeklyUp.Application.Billing.Commands.CreateBillingPortalSession;
using WeeklyUp.Application.Billing.Commands.CreateCheckoutSession;
using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Domain.Enums;

namespace WeeklyUp.Api.Modules;

public sealed record CreateCheckoutSessionRequest(PlanType Plan, string SuccessUrl, string CancelUrl);

public sealed record BillingPortalRequest(string ReturnUrl);

public sealed class BillingModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/billing")
            .WithTags("Billing")
            .RequireRateLimiting("api")
            .RequireAuthorization();

        group.MapPost("/checkout-session", async (
            CreateCheckoutSessionRequest request,
            IMediator mediator,
            ICurrentUserService currentUser,
            CancellationToken ct) =>
        {
            var command = new CreateCheckoutSessionCommand(
                currentUser.UserId,
                request.Plan,
                request.SuccessUrl,
                request.CancelUrl);
            var result = await mediator.Send(command, ct);
            return result.Match(
                dto => Results.Ok(dto),
                error => error.ToProblem());
        })
        .WithName("CreateCheckoutSession")
        .WithSummary("Cria sessao de checkout do Stripe para upgrade de plano")
        .Produces<CheckoutSessionDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/portal", async (
            BillingPortalRequest request,
            IMediator mediator,
            ICurrentUserService currentUser,
            CancellationToken ct) =>
        {
            var command = new CreateBillingPortalSessionCommand(currentUser.UserId, request.ReturnUrl);
            var result = await mediator.Send(command, ct);
            return result.Match(
                dto => Results.Ok(dto),
                error => error.ToProblem());
        })
        .WithName("CreateBillingPortalSession")
        .WithSummary("Cria sessao do portal de faturamento Stripe")
        .Produces<BillingPortalSessionDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .Produces(StatusCodes.Status401Unauthorized);
    }
}
