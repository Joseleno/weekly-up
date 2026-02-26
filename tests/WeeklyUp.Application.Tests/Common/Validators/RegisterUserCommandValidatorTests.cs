using FluentAssertions;
using FluentValidation.TestHelper;
using WeeklyUp.Application.Users.Commands.RegisterUser;
using WeeklyUp.Domain.Enums;

namespace WeeklyUp.Application.Tests.Common.Validators;

public sealed class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator _sut = new();

    [Fact]
    public void Validate_WhenEmailIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new RegisterUserCommand("", "Test User", "Test Business", BusinessType.Ecommerce);

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_WhenEmailIsInvalid_ShouldHaveError()
    {
        // Arrange
        var command = new RegisterUserCommand("not-an-email", "Test User", "Test Business", BusinessType.Ecommerce);

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_WhenEmailExceedsMaxLength_ShouldHaveError()
    {
        // Arrange
        var longEmail = new string('a', 250) + "@b.com";
        var command = new RegisterUserCommand(longEmail, "Test User", "Test Business", BusinessType.Ecommerce);

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_WhenNameIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new RegisterUserCommand("test@example.com", "", "Test Business", BusinessType.Ecommerce);

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenBusinessNameIsTooShort_ShouldHaveError()
    {
        // Arrange
        var command = new RegisterUserCommand("test@example.com", "Test User", "A", BusinessType.Ecommerce);

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.BusinessName);
    }

    [Fact]
    public void Validate_WhenAllFieldsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new RegisterUserCommand("test@example.com", "Test User", "Test Business", BusinessType.Ecommerce);

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
