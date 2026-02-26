using FluentAssertions;

using Mediator;

using Microsoft.Extensions.Logging;

using NSubstitute;

using WeeklyUp.Application.Common.Behaviors;

namespace WeeklyUp.Application.Tests.Common.Behaviors;

public sealed record LoggingTestMessage(string Value) : IMessage;

public sealed class LoggingBehaviorTests
{
    [Fact]
    public async Task Handle_ShouldAlwaysCallNext()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingBehavior<LoggingTestMessage, string>>>();
        var behavior = new LoggingBehavior<LoggingTestMessage, string>(logger);
        var message = new LoggingTestMessage("test");
        var nextCalled = false;
        MessageHandlerDelegate<LoggingTestMessage, string> next = (_, _) =>
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
    public async Task Handle_WhenExecutionExceeds500ms_ShouldLogWarning()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingBehavior<LoggingTestMessage, string>>>();
        var behavior = new LoggingBehavior<LoggingTestMessage, string>(logger);
        var message = new LoggingTestMessage("slow");
        MessageHandlerDelegate<LoggingTestMessage, string> slowNext = async (_, ct) =>
        {
            await Task.Delay(600, ct);
            return "slow-result";
        };

        // Act
        var response = await behavior.Handle(message, CancellationToken.None, slowNext);

        // Assert
        response.Should().Be("slow-result");
        logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Slow request")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }
}
