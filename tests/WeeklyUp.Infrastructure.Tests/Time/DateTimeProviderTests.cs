using FluentAssertions;

using WeeklyUp.Infrastructure.Time;

namespace WeeklyUp.Infrastructure.Tests.Time;

public sealed class DateTimeProviderTests
{
    [Fact]
    public void UtcNow_ShouldReturnCurrentUtcTime()
    {
        // Arrange
        var provider = new DateTimeProvider();
        var before = DateTime.UtcNow;

        // Act
        var utcNow = provider.UtcNow;

        // Assert
        var after = DateTime.UtcNow;
        utcNow.Should().BeOnOrAfter(before);
        utcNow.Should().BeOnOrBefore(after);
        utcNow.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Today_ShouldReturnCurrentDate()
    {
        // Arrange
        var provider = new DateTimeProvider();
        var expected = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var today = provider.Today;

        // Assert
        today.Should().Be(expected);
    }
}
