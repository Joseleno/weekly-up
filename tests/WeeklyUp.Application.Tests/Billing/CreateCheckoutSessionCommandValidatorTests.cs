using FluentAssertions;
using FluentValidation.TestHelper;

using WeeklyUp.Application.Billing.Commands.CreateCheckoutSession;
using WeeklyUp.Domain.Enums;

namespace WeeklyUp.Application.Tests.Billing;

public sealed class CreateCheckoutSessionCommandValidatorTests
{
    private readonly CreateCheckoutSessionCommandValidator _sut = new();

    [Fact]
    public async Task Validate_WhenAllFieldsValid_HasNoErrors()
    {
        // Arrange
        var command = new CreateCheckoutSessionCommand(
            Guid.NewGuid(), PlanType.Pro, "https://success.test", "https://cancel.test");

        // Act
        var result = await _sut.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenUserIdEmpty_HasError()
    {
        // Arrange
        var command = new CreateCheckoutSessionCommand(
            Guid.Empty, PlanType.Pro, "https://success.test", "https://cancel.test");

        // Act
        var result = await _sut.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task Validate_WhenPlanIsFree_HasError()
    {
        // Arrange
        var command = new CreateCheckoutSessionCommand(
            Guid.NewGuid(), PlanType.Free, "https://success.test", "https://cancel.test");

        // Act
        var result = await _sut.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Plan);
    }

    [Fact]
    public async Task Validate_WhenSuccessUrlEmpty_HasError()
    {
        // Arrange
        var command = new CreateCheckoutSessionCommand(
            Guid.NewGuid(), PlanType.Pro, "", "https://cancel.test");

        // Act
        var result = await _sut.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.SuccessUrl);
    }

    [Fact]
    public async Task Validate_WhenSuccessUrlIsHttp_HasError()
    {
        // Arrange
        var command = new CreateCheckoutSessionCommand(
            Guid.NewGuid(), PlanType.Pro, "http://insecure.test", "https://cancel.test");

        // Act
        var result = await _sut.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.SuccessUrl);
    }

    [Fact]
    public async Task Validate_WhenSuccessUrlNotAbsolute_HasError()
    {
        // Arrange
        var command = new CreateCheckoutSessionCommand(
            Guid.NewGuid(), PlanType.Pro, "not-a-url", "https://cancel.test");

        // Act
        var result = await _sut.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.SuccessUrl);
    }

    [Fact]
    public async Task Validate_WhenCancelUrlEmpty_HasError()
    {
        // Arrange
        var command = new CreateCheckoutSessionCommand(
            Guid.NewGuid(), PlanType.Pro, "https://success.test", "");

        // Act
        var result = await _sut.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.CancelUrl);
    }

    [Fact]
    public async Task Validate_WhenCancelUrlIsHttp_HasError()
    {
        // Arrange
        var command = new CreateCheckoutSessionCommand(
            Guid.NewGuid(), PlanType.Pro, "https://success.test", "http://insecure.test");

        // Act
        var result = await _sut.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.CancelUrl);
    }
}
