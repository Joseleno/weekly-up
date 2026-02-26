using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Mediator;
using NSubstitute;
using WeeklyUp.Application.Common.Behaviors;

namespace WeeklyUp.Application.Tests.Common.Behaviors;

public sealed record ValidationTestMessage(string Value) : IMessage;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WhenNoValidators_ShouldCallNext()
    {
        // Arrange
        var behavior = new ValidationBehavior<ValidationTestMessage, string>(Enumerable.Empty<IValidator<ValidationTestMessage>>());
        var message = new ValidationTestMessage("test");
        var nextCalled = false;
        MessageHandlerDelegate<ValidationTestMessage, string> next = (_, _) =>
        {
            nextCalled = true;
            return new ValueTask<string>("result");
        };

        // Act
        var response = await behavior.Handle(message, CancellationToken.None, next);

        // Assert
        nextCalled.Should().BeTrue();
        response.Should().Be("result");
    }

    [Fact]
    public async Task Handle_WhenValidationPasses_ShouldCallNext()
    {
        // Arrange
        var validator = Substitute.For<IValidator<ValidationTestMessage>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<ValidationTestMessage>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        var behavior = new ValidationBehavior<ValidationTestMessage, string>(new[] { validator });
        var message = new ValidationTestMessage("test");
        var nextCalled = false;
        MessageHandlerDelegate<ValidationTestMessage, string> next = (_, _) =>
        {
            nextCalled = true;
            return new ValueTask<string>("ok");
        };

        // Act
        var response = await behavior.Handle(message, CancellationToken.None, next);

        // Assert
        nextCalled.Should().BeTrue();
        response.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_WhenValidationFails_ShouldThrowValidationException()
    {
        // Arrange
        var failure = new ValidationFailure("Value", "Value is required");
        var validationResult = new ValidationResult(new[] { failure });
        var validator = Substitute.For<IValidator<ValidationTestMessage>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<ValidationTestMessage>>(), Arg.Any<CancellationToken>())
            .Returns(validationResult);
        var behavior = new ValidationBehavior<ValidationTestMessage, string>(new[] { validator });
        var message = new ValidationTestMessage("");
        MessageHandlerDelegate<ValidationTestMessage, string> next = (_, _) => new ValueTask<string>("never");

        // Act
        Func<Task> act = async () => await behavior.Handle(message, CancellationToken.None, next);

        // Assert
        await act.Should().ThrowAsync<WeeklyUp.Application.Common.Exceptions.ValidationException>();
    }
}
