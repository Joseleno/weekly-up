using FluentAssertions;
using FluentValidation.TestHelper;

using WeeklyUp.Application.Billing.Commands.CreateBillingPortalSession;

namespace WeeklyUp.Application.Tests.Billing;

public sealed class CreateBillingPortalSessionCommandValidatorTests
{
    private readonly CreateBillingPortalSessionCommandValidator _sut = new();

    [Fact]
    public async Task Validate_WhenAllFieldsValid_HasNoErrors()
    {
        // Arrange
        var command = new CreateBillingPortalSessionCommand(Guid.NewGuid(), "https://return.test");

        // Act
        var result = await _sut.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenUserIdEmpty_HasError()
    {
        // Arrange
        var command = new CreateBillingPortalSessionCommand(Guid.Empty, "https://return.test");

        // Act
        var result = await _sut.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task Validate_WhenReturnUrlEmpty_HasError()
    {
        // Arrange
        var command = new CreateBillingPortalSessionCommand(Guid.NewGuid(), "");

        // Act
        var result = await _sut.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.ReturnUrl);
    }

    [Fact]
    public async Task Validate_WhenReturnUrlIsHttp_HasError()
    {
        // Arrange
        var command = new CreateBillingPortalSessionCommand(Guid.NewGuid(), "http://insecure.test");

        // Act
        var result = await _sut.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.ReturnUrl);
    }

    [Fact]
    public async Task Validate_WhenReturnUrlNotAbsolute_HasError()
    {
        // Arrange
        var command = new CreateBillingPortalSessionCommand(Guid.NewGuid(), "not-a-url");

        // Act
        var result = await _sut.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.ReturnUrl);
    }
}
