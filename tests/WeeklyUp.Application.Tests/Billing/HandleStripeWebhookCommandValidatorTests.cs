using FluentValidation.TestHelper;

using WeeklyUp.Application.Billing;
using WeeklyUp.Application.Billing.Commands.HandleStripeWebhook;

namespace WeeklyUp.Application.Tests.Billing;

public sealed class HandleStripeWebhookCommandValidatorTests
{
    private readonly HandleStripeWebhookCommandValidator _sut = new();

    [Fact]
    public async Task Validate_WhenAllFieldsValid_HasNoErrors()
    {
        // Arrange
        var command = new HandleStripeWebhookCommand(
            new StripeWebhookEvent("customer.subscription.updated", "evt_123", null, null));

        // Act
        var result = await _sut.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenTypeEmpty_HasError()
    {
        // Arrange
        var command = new HandleStripeWebhookCommand(
            new StripeWebhookEvent("", "evt_123", null, null));

        // Act
        var result = await _sut.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor("WebhookEvent.Type");
    }

    [Fact]
    public async Task Validate_WhenIdEmpty_HasError()
    {
        // Arrange
        var command = new HandleStripeWebhookCommand(
            new StripeWebhookEvent("customer.subscription.updated", "", null, null));

        // Act
        var result = await _sut.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor("WebhookEvent.Id");
    }
}
