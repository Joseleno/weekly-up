using Mediator;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using WeeklyUp.Application.Common.Settings;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.Interfaces;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Billing.Commands.HandleStripeWebhook;

public sealed partial class HandleStripeWebhookCommandHandler
    : ICommandHandler<HandleStripeWebhookCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;
    private readonly StripeSettings _stripeSettings;
    private readonly ILogger<HandleStripeWebhookCommandHandler> _logger;

    public HandleStripeWebhookCommandHandler(
        IUnitOfWork uow,
        IOptions<StripeSettings> stripeSettings,
        ILogger<HandleStripeWebhookCommandHandler> logger)
    {
        _uow = uow;
        _stripeSettings = stripeSettings.Value;
        _logger = logger;
    }

    public async ValueTask<Result<bool>> Handle(
        HandleStripeWebhookCommand command,
        CancellationToken cancellationToken)
    {
        StripeWebhookEvent webhookEvent = command.WebhookEvent;

        switch (webhookEvent.Type)
        {
            case "customer.subscription.updated":
                await HandleSubscriptionUpdatedAsync(webhookEvent, cancellationToken);
                break;

            case "customer.subscription.deleted":
                await HandleSubscriptionDeletedAsync(webhookEvent, cancellationToken);
                break;

            default:
                LogEventIgnored(webhookEvent.Type);
                break;
        }

        return true;
    }

    private async Task HandleSubscriptionUpdatedAsync(StripeWebhookEvent webhookEvent, CancellationToken ct)
    {
        if (!TryGetUserId(webhookEvent.UserId, out Guid userId))
        {
            LogNoUserIdUpdated(webhookEvent.Id);
            return;
        }

        PlanType? newPlan = TryResolvePlan(webhookEvent.PriceId);
        if (newPlan is null)
        {
            LogUnknownPriceId(webhookEvent.PriceId, webhookEvent.Id);
            return;
        }

        await ApplyPlanChangeAsync(userId, newPlan.Value, ct);
    }

    private async Task HandleSubscriptionDeletedAsync(StripeWebhookEvent webhookEvent, CancellationToken ct)
    {
        if (!TryGetUserId(webhookEvent.UserId, out Guid userId))
        {
            LogNoUserIdDeleted(webhookEvent.Id);
            return;
        }

        await ApplyPlanChangeAsync(userId, PlanType.Free, ct);
    }

    private async Task ApplyPlanChangeAsync(Guid userId, PlanType newPlan, CancellationToken ct)
    {
        User? user = await _uow.Users.GetByIdAsync(userId, ct);
        if (user is null)
        {
            LogUserNotFound(userId);
            return;
        }

        if (user.Plan == newPlan)
        {
            return;
        }

        user.SetPlanFromWebhook(newPlan);
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(ct);

        LogPlanUpdated(userId, newPlan);
    }

    private static bool TryGetUserId(string? raw, out Guid userId)
    {
        userId = Guid.Empty;
        return !string.IsNullOrWhiteSpace(raw) && Guid.TryParse(raw, out userId);
    }

    private PlanType? TryResolvePlan(string? priceId) => priceId switch
    {
        _ when priceId == _stripeSettings.ProPriceId => PlanType.Pro,
        _ when priceId == _stripeSettings.BusinessPriceId => PlanType.Business,
        _ => null,
    };

    [LoggerMessage(Level = LogLevel.Debug, Message = "Evento Stripe ignorado: {EventType}")]
    private partial void LogEventIgnored(string eventType);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Webhook subscription.updated sem userId. EventId: {Id}")]
    private partial void LogNoUserIdUpdated(string id);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Webhook: priceId desconhecido {PriceId}. EventId: {Id}")]
    private partial void LogUnknownPriceId(string? priceId, string id);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Webhook subscription.deleted sem userId. EventId: {Id}")]
    private partial void LogNoUserIdDeleted(string id);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Webhook: usuario {UserId} nao encontrado.")]
    private partial void LogUserNotFound(Guid userId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Plano atualizado via webhook: usuario {UserId} plano {Plan}")]
    private partial void LogPlanUpdated(Guid userId, PlanType plan);
}
