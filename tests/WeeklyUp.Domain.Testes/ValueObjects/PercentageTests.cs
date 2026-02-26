using FluentAssertions;
using WeeklyUp.Domain.ValueObjects;

namespace WeeklyUp.Domain.Testes.ValueObjects;

public sealed class PercentageTests
{
    [Fact]
    public void FromValue_DecimalWithMoreThanOnePlace_RoundsToOneDecimal()
    {
        // Act
        var pct = Percentage.FromValue(33.333m);

        // Assert
        pct.Value.Should().Be(33.3m);
    }

    [Fact]
    public void CalculateChange_GrowthFromHundredToHundredFifty_ReturnsFiftyPercent()
    {
        // Act
        var pct = Percentage.CalculateChange(current: 150m, previous: 100m);

        // Assert
        pct.Value.Should().Be(50.0m);
        pct.IsPositive.Should().BeTrue();
        pct.IsNegative.Should().BeFalse();
    }

    [Fact]
    public void CalculateChange_DeclineFromHundredToFifty_ReturnsNegativeFiftyPercent()
    {
        // Act
        var pct = Percentage.CalculateChange(current: 50m, previous: 100m);

        // Assert
        pct.Value.Should().Be(-50.0m);
        pct.IsNegative.Should().BeTrue();
        pct.IsPositive.Should().BeFalse();
    }

    [Fact]
    public void CalculateChange_PreviousZeroAndCurrentPositive_ReturnsHundredPercent()
    {
        // Act
        var pct = Percentage.CalculateChange(current: 100m, previous: 0m);

        // Assert
        pct.Value.Should().Be(100.0m);
    }

    [Fact]
    public void CalculateChange_BothZero_ReturnsZeroPercent()
    {
        // Act
        var pct = Percentage.CalculateChange(current: 0m, previous: 0m);

        // Assert
        pct.Value.Should().Be(0m);
    }

    [Fact]
    public void IsPositive_ZeroValue_ReturnsFalse()
    {
        // Arrange
        var pct = Percentage.FromValue(0m);

        // Assert
        pct.IsPositive.Should().BeFalse();
        pct.IsNegative.Should().BeFalse();
    }

    [Fact]
    public void Equals_SameValue_ReturnsTrue()
    {
        // Arrange
        var p1 = Percentage.FromValue(50m);
        var p2 = Percentage.FromValue(50m);

        // Assert
        p1.Should().Be(p2);
    }

    [Fact]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        // Arrange
        var p1 = Percentage.FromValue(50m);
        var p2 = Percentage.FromValue(75m);

        // Assert
        p1.Should().NotBe(p2);
    }

    [Fact]
    public void ToString_FormatsWithPercentSignUsingCurrentCulture()
    {
        // Arrange
        var pct = Percentage.FromValue(25.5m);

        // Act
        var text = pct.ToString();

        // Assert — formato N1 usa separador decimal da cultura corrente (ex: "25,5%" em pt-BR ou "25.5%" em en-US)
        text.Should().EndWith("%");
        text.Should().Contain("25");
        text.Should().Contain("5");
    }

    [Fact]
    public void CalculateChange_SameValues_ReturnsZeroPercent()
    {
        // Act
        var pct = Percentage.CalculateChange(current: 100m, previous: 100m);

        // Assert
        pct.Value.Should().Be(0m);
    }
}
