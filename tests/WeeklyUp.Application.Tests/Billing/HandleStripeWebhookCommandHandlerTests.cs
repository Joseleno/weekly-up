using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using WeeklyUp.Application.Billing;
using WeeklyUp.Application.Billing.Commands.HandleStripeWebhook;
using WeeklyUp.Application.Common.Settings;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.Interfaces;
using WeeklyUp.Domain.Interfaces.Repositories;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Tests.Billing;

public sealed class HandleStripeWebhookCommandHandlerTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly HandleStripeWebhookCommandHandler _sut;

    private static readonly StripeSettings DefaultSettings = new()
    {
        ProPriceId = "price_pro_test",
        BusinessPriceId = "price_business_test",
    };

    public HandleStripeWebhookCommandHandlerTests()
    {
        _uow.Users.Returns(_users);
        _sut = new HandleStripeWebhookCommandHandler(
            _uow,
            Options.Create(DefaultSettings),
            NullLogger<HandleStripeWebhookCommandHandler>.Instance);
    }

    // ─── subscription.updated ────────────────────────────────────────────────

    [Fact]
    public async Task Handle_SubscriptionUpdated_WhenValidUserAndPrice_UpdatesPlan()
    {
        // Arrange
        var user = User.Create("test@example.com", "Test User", "Test Business", BusinessType.Ecommerce).Value;
        var webhookEvent = new StripeWebhookEvent(
            "customer.subscription.updated", "evt_1", user.Id.ToString(), DefaultSettings.ProPriceId);

        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        Result<bool> result = await _sut.Handle(
            new HandleStripeWebhookCommand(webhookEvent), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.Plan.Should().Be(PlanType.Pro);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SubscriptionUpdated_WhenUnknownPriceId_DoesNotChangePlan()
    {
        // Arrange
        var user = User.Create("test@example.com", "Test User", "Test Business", BusinessType.Ecommerce).Value;
        var webhookEvent = new StripeWebhookEvent(
            "customer.subscription.updated", "evt_1", user.Id.ToString(), "price_unknown_xyz");

        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        Result<bool> result = await _sut.Handle(
            new HandleStripeWebhookCommand(webhookEvent), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.Plan.Should().Be(PlanType.Free); // unchanged
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SubscriptionUpdated_WhenUserNotFound_ReturnsSuccessWithoutSaving()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var webhookEvent = new StripeWebhookEvent(
            "customer.subscription.updated", "evt_1", userId.ToString(), DefaultSettings.ProPriceId);

        _users.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        Result<bool> result = await _sut.Handle(
            new HandleStripeWebhookCommand(webhookEvent), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue(); // Webhook handler sempre retorna sucesso (para Stripe não reenviar)
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SubscriptionUpdated_WhenNoUserId_ReturnsSuccessWithoutSaving()
    {
        // Arrange — evento sem userId (metadata ausente no Stripe)
        var webhookEvent = new StripeWebhookEvent(
            "customer.subscription.updated", "evt_1", null, DefaultSettings.ProPriceId);

        // Act
        Result<bool> result = await _sut.Handle(
            new HandleStripeWebhookCommand(webhookEvent), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SubscriptionUpdated_WhenPlanAlreadySame_SkipsSave_Idempotent()
    {
        // Arrange — simula webhook duplicado com mesmo plano
        var user = User.Create("test@example.com", "Test User", "Test Business", BusinessType.Ecommerce).Value;
        user.SetPlanFromWebhook(PlanType.Pro); // já está no Pro
        var webhookEvent = new StripeWebhookEvent(
            "customer.subscription.updated", "evt_1", user.Id.ToString(), DefaultSettings.ProPriceId);

        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        Result<bool> result = await _sut.Handle(
            new HandleStripeWebhookCommand(webhookEvent), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ─── subscription.deleted ────────────────────────────────────────────────

    [Fact]
    public async Task Handle_SubscriptionDeleted_WhenUserOnPaidPlan_DowngradesToFree()
    {
        // Arrange
        var user = User.Create("test@example.com", "Test User", "Test Business", BusinessType.Ecommerce).Value;
        user.SetPlanFromWebhook(PlanType.Pro);
        var webhookEvent = new StripeWebhookEvent(
            "customer.subscription.deleted", "evt_2", user.Id.ToString(), null);

        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        Result<bool> result = await _sut.Handle(
            new HandleStripeWebhookCommand(webhookEvent), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.Plan.Should().Be(PlanType.Free);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SubscriptionDeleted_WhenAlreadyFree_SkipsSave_Idempotent()
    {
        // Arrange — usuário já é Free (webhook duplicado)
        var user = User.Create("test@example.com", "Test User", "Test Business", BusinessType.Ecommerce).Value;
        var webhookEvent = new StripeWebhookEvent(
            "customer.subscription.deleted", "evt_2", user.Id.ToString(), null);

        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        Result<bool> result = await _sut.Handle(
            new HandleStripeWebhookCommand(webhookEvent), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ─── unknown event type ───────────────────────────────────────────────────

    [Fact]
    public async Task Handle_UnknownEventType_ReturnsSuccessWithoutSaving()
    {
        // Arrange
        var webhookEvent = new StripeWebhookEvent("payment_intent.succeeded", "evt_3", null, null);

        // Act
        Result<bool> result = await _sut.Handle(
            new HandleStripeWebhookCommand(webhookEvent), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
