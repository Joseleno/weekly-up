using Mediator;

using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Billing.Commands.HandleStripeWebhook;

public sealed record HandleStripeWebhookCommand(StripeWebhookEvent WebhookEvent) : ICommand<Result<bool>>;
