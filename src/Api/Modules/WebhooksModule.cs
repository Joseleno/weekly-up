using Carter;

using Mediator;

using WeeklyUp.Application.Billing.Commands.HandleStripeWebhook;
using WeeklyUp.Application.Common.Interfaces;

namespace WeeklyUp.Api.Modules;

public sealed class WebhooksModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/webhooks/stripe", async (
            HttpRequest request,
            IStripeService stripeService,
            IMediator mediator,
            CancellationToken ct) =>
        {
            request.EnableBuffering();
            using var reader = new StreamReader(request.Body, leaveOpen: true);
            string payload = await reader.ReadToEndAsync(ct);
            string? signature = request.Headers["Stripe-Signature"];

            if (string.IsNullOrWhiteSpace(signature))
            {
                return Results.BadRequest("Stripe-Signature header ausente.");
            }

            var parseResult = stripeService.ParseWebhookEvent(payload, signature);
            if (parseResult.IsFailure)
            {
                return Results.BadRequest(parseResult.Error.Message);
            }

            // Sempre retorna 200: falhas de negócio não devem causar reenvio pelo Stripe
            _ = await mediator.Send(new HandleStripeWebhookCommand(parseResult.Value), ct);
            return Results.Ok();
        })
        .WithTags("Webhooks")
        .WithName("HandleStripeWebhook")
        .WithSummary("Recebe eventos do Stripe via webhook")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .RequireRateLimiting("webhook");
    }
}
