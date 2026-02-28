using FluentAssertions;

using Microsoft.Extensions.Logging;

using WeeklyUp.Application.Common.Notifications;
using WeeklyUp.Application.Tests.Infrastructure;
using WeeklyUp.Application.Users.Events;
using WeeklyUp.Domain.Events;

namespace WeeklyUp.Application.Tests.Users.Events;

public sealed class UserEmailVerifiedNotificationHandlerTests
{
    private readonly CapturingLogger<UserEmailVerifiedNotificationHandler> _logger = new();
    private readonly UserEmailVerifiedNotificationHandler _sut;

    public UserEmailVerifiedNotificationHandlerTests()
        => _sut = new UserEmailVerifiedNotificationHandler(_logger);

    [Fact]
    public async Task Handle_WhenUserEmailVerifiedEvent_LogsInformation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var evt = new UserEmailVerifiedEvent(userId);
        var notification = new DomainEventNotification(evt);

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        _logger.HasEntry(LogLevel.Information).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenEventIsNotUserEmailVerified_DoesNotLog()
    {
        // Arrange
        var otherEvent = new UserRegisteredEvent(Guid.NewGuid(), "user@test.com", "Test User", null);
        var notification = new DomainEventNotification(otherEvent);

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        _logger.Entries.Should().BeEmpty();
    }
}
