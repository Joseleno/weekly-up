using FluentAssertions;

using Microsoft.Extensions.Logging;

using NSubstitute;

using WeeklyUp.Application.Common.Notifications;
using WeeklyUp.Application.Tests.Infrastructure;
using WeeklyUp.Application.Users.Events;
using WeeklyUp.Domain.Events;
using WeeklyUp.Domain.Interfaces.Services;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Tests.Users.Events;

public sealed class UserRegisteredNotificationHandlerTests
{
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly CapturingLogger<UserRegisteredNotificationHandler> _logger = new();
    private readonly UserRegisteredNotificationHandler _sut;

    public UserRegisteredNotificationHandlerTests()
    {
        _sut = new UserRegisteredNotificationHandler(_emailSender, _logger);

        _emailSender.SendWelcomeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(true));
        _emailSender.SendVerificationEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(true));
    }

    [Fact]
    public async Task Handle_WhenUserRegisteredEvent_SendsWelcomeEmail()
    {
        // Arrange
        var evt = new UserRegisteredEvent(Guid.NewGuid(), "user@test.com", "Test User", "token-abc-123");
        var notification = new DomainEventNotification(evt);

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        await _emailSender.Received(1).SendWelcomeAsync(evt.Email, evt.Name, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserRegisteredEvent_SendsVerificationEmail()
    {
        // Arrange
        var evt = new UserRegisteredEvent(Guid.NewGuid(), "user@test.com", "Test User", "token-abc-123");
        var notification = new DomainEventNotification(evt);

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        await _emailSender.Received(1).SendVerificationEmailAsync(
            evt.Email, evt.Name, evt.VerificationToken!, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenEventIsNotUserRegistered_DoesNothing()
    {
        // Arrange
        var otherEvent = new UserEmailVerifiedEvent(Guid.NewGuid());
        var notification = new DomainEventNotification(otherEvent);

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        await _emailSender.DidNotReceive().SendWelcomeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserHasNoVerificationToken_SendsOnlyWelcomeEmail()
    {
        // Arrange
        var evt = new UserRegisteredEvent(Guid.NewGuid(), "user@test.com", "Test User", null);
        var notification = new DomainEventNotification(evt);

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        await _emailSender.Received(1).SendWelcomeAsync(evt.Email, evt.Name, Arg.Any<CancellationToken>());
        await _emailSender.DidNotReceive().SendVerificationEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenWelcomeEmailFails_LogsError()
    {
        // Arrange
        _emailSender.SendWelcomeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<bool>(AppError.Failure("Email.SendFailed", "SMTP error")));

        var evt = new UserRegisteredEvent(Guid.NewGuid(), "user@test.com", "Test User", "token-abc");
        var notification = new DomainEventNotification(evt);

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        _logger.HasEntry(LogLevel.Error).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenVerificationEmailFails_LogsError()
    {
        // Arrange
        _emailSender.SendVerificationEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<bool>(AppError.Failure("Email.SendFailed", "SMTP error")));

        var evt = new UserRegisteredEvent(Guid.NewGuid(), "user@test.com", "Test User", "token-abc");
        var notification = new DomainEventNotification(evt);

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        _logger.HasEntry(LogLevel.Error).Should().BeTrue();
    }
}
