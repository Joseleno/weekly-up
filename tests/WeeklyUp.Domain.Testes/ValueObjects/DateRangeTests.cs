using FluentAssertions;
using WeeklyUp.Domain.ValueObjects;

namespace WeeklyUp.Domain.Testes.ValueObjects;

public sealed class DateRangeTests
{
    [Fact]
    public void Create_ValidSixDayRange_ReturnsSuccess()
    {
        // Arrange
        var start = new DateOnly(2025, 1, 6);
        var end = new DateOnly(2025, 1, 12);

        // Act
        var result = DateRange.Create(start, end);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Start.Should().Be(start);
        result.Value.End.Should().Be(end);
    }

    [Fact]
    public void Create_EndBeforeStart_ReturnsFailure()
    {
        // Arrange
        var start = new DateOnly(2025, 1, 12);
        var end = new DateOnly(2025, 1, 6);

        // Act
        var result = DateRange.Create(start, end);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DateRange.EndBeforeStart");
    }

    [Fact]
    public void Create_SevenDayDifference_ReturnsFailure()
    {
        // Arrange — diferenca de 7 dias excede o maximo de 6
        var start = new DateOnly(2025, 1, 6);
        var end = new DateOnly(2025, 1, 13);

        // Act
        var result = DateRange.Create(start, end);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DateRange.TooLong");
    }

    [Fact]
    public void Create_SameDay_ReturnsSuccess()
    {
        // Arrange
        var date = new DateOnly(2025, 1, 6);

        // Act
        var result = DateRange.Create(date, date);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Start.Should().Be(date);
        result.Value.End.Should().Be(date);
    }

    [Fact]
    public void Create_ExactlyMaxSixDayDifference_ReturnsSuccess()
    {
        // Arrange
        var start = new DateOnly(2025, 1, 6);
        var end = new DateOnly(2025, 1, 12); // diferenca = 6 dias (limite exato)

        // Act
        var result = DateRange.Create(start, end);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void PreviousWeek_ReturnsRangeStartingOnMonday()
    {
        // Act
        var range = DateRange.PreviousWeek();

        // Assert
        range.Start.DayOfWeek.Should().Be(DayOfWeek.Monday);
    }

    [Fact]
    public void PreviousWeek_ReturnsRangeEndingOnSunday()
    {
        // Act
        var range = DateRange.PreviousWeek();

        // Assert
        range.End.DayOfWeek.Should().Be(DayOfWeek.Sunday);
    }

    [Fact]
    public void PreviousWeek_ReturnsExactlySixDayDifference()
    {
        // Act
        var range = DateRange.PreviousWeek();

        // Assert
        (range.End.DayNumber - range.Start.DayNumber).Should().Be(6);
    }

    [Fact]
    public void CurrentWeek_ReturnsRangeStartingOnMonday()
    {
        // Act
        var range = DateRange.CurrentWeek();

        // Assert
        range.Start.DayOfWeek.Should().Be(DayOfWeek.Monday);
    }

    [Fact]
    public void CurrentWeek_ReturnsRangeEndingOnSunday()
    {
        // Act
        var range = DateRange.CurrentWeek();

        // Assert
        range.End.DayOfWeek.Should().Be(DayOfWeek.Sunday);
    }

    [Fact]
    public void Contains_DateWithinRange_ReturnsTrue()
    {
        // Arrange
        var range = DateRange.Create(new DateOnly(2025, 1, 6), new DateOnly(2025, 1, 12)).Value;

        // Act
        var contains = range.Contains(new DateOnly(2025, 1, 9));

        // Assert
        contains.Should().BeTrue();
    }

    [Fact]
    public void Contains_DateAfterRange_ReturnsFalse()
    {
        // Arrange
        var range = DateRange.Create(new DateOnly(2025, 1, 6), new DateOnly(2025, 1, 12)).Value;

        // Act
        var contains = range.Contains(new DateOnly(2025, 1, 13));

        // Assert
        contains.Should().BeFalse();
    }

    [Fact]
    public void Contains_DateBeforeRange_ReturnsFalse()
    {
        // Arrange
        var range = DateRange.Create(new DateOnly(2025, 1, 6), new DateOnly(2025, 1, 12)).Value;

        // Act
        var contains = range.Contains(new DateOnly(2025, 1, 5));

        // Assert
        contains.Should().BeFalse();
    }

    [Fact]
    public void Contains_StartDate_ReturnsTrue()
    {
        // Arrange
        var start = new DateOnly(2025, 1, 6);
        var range = DateRange.Create(start, new DateOnly(2025, 1, 12)).Value;

        // Act
        var contains = range.Contains(start);

        // Assert
        contains.Should().BeTrue();
    }

    [Fact]
    public void Contains_EndDate_ReturnsTrue()
    {
        // Arrange
        var end = new DateOnly(2025, 1, 12);
        var range = DateRange.Create(new DateOnly(2025, 1, 6), end).Value;

        // Act
        var contains = range.Contains(end);

        // Assert
        contains.Should().BeTrue();
    }

    [Fact]
    public void Equals_SameStartAndEnd_ReturnsTrue()
    {
        // Arrange
        var r1 = DateRange.Create(new DateOnly(2025, 1, 6), new DateOnly(2025, 1, 12)).Value;
        var r2 = DateRange.Create(new DateOnly(2025, 1, 6), new DateOnly(2025, 1, 12)).Value;

        // Assert
        r1.Should().Be(r2);
    }

    [Fact]
    public void Equals_DifferentStart_ReturnsFalse()
    {
        // Arrange
        var r1 = DateRange.Create(new DateOnly(2025, 1, 6), new DateOnly(2025, 1, 12)).Value;
        var r2 = DateRange.Create(new DateOnly(2025, 1, 7), new DateOnly(2025, 1, 12)).Value;

        // Assert
        r1.Should().NotBe(r2);
    }

    [Fact]
    public void ToString_FormatsAsYearMonthDay()
    {
        // Arrange
        var range = DateRange.Create(new DateOnly(2025, 1, 6), new DateOnly(2025, 1, 12)).Value;

        // Assert
        range.ToString().Should().Be("2025-01-06 a 2025-01-12");
    }
}
