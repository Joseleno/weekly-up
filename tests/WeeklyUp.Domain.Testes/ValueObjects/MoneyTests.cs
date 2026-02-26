using FluentAssertions;

using WeeklyUp.Domain.ValueObjects;

namespace WeeklyUp.Domain.Testes.ValueObjects;

public sealed class MoneyTests
{
    [Fact]
    public void BRL_ValidAmount_RoundsTwoDecimalPlaces()
    {
        // Act
        var money = Money.BRL(10.999m);

        // Assert
        money.Amount.Should().Be(11.00m);
        money.Currency.Should().Be("BRL");
    }

    [Fact]
    public void Zero_HasZeroAmountAndBRLCurrency()
    {
        // Assert
        Money.Zero.Amount.Should().Be(0m);
        Money.Zero.Currency.Should().Be("BRL");
    }

    [Fact]
    public void Add_SameCurrency_ReturnsCorrectSum()
    {
        // Arrange
        var a = Money.BRL(10.00m);
        var b = Money.BRL(5.50m);

        // Act
        var result = a.Add(b);

        // Assert
        result.Amount.Should().Be(15.50m);
        result.Currency.Should().Be("BRL");
    }

    [Fact]
    public void Subtract_SameCurrency_ReturnsCorrectDifference()
    {
        // Arrange
        var a = Money.BRL(10.00m);
        var b = Money.BRL(3.00m);

        // Act
        var result = a.Subtract(b);

        // Assert
        result.Amount.Should().Be(7.00m);
        result.Currency.Should().Be("BRL");
    }

    [Fact]
    public void Equals_SameAmountAndCurrency_ReturnsTrue()
    {
        // Arrange
        var m1 = Money.BRL(100m);
        var m2 = Money.BRL(100m);

        // Assert
        m1.Should().Be(m2);
    }

    [Fact]
    public void Equals_DifferentAmount_ReturnsFalse()
    {
        // Arrange
        var m1 = Money.BRL(100m);
        var m2 = Money.BRL(200m);

        // Assert
        m1.Should().NotBe(m2);
    }

    [Fact]
    public void ToString_FormatsWithCurrencyAndAmount()
    {
        // Arrange
        var money = Money.BRL(1234.56m);

        // Assert
        money.ToString().Should().StartWith("BRL ");
        money.ToString().Should().Contain("1");
        money.ToString().Should().Contain("234");
        money.ToString().Should().Contain("56");
    }

    [Fact]
    public void BRL_NegativeAmount_StoresNegativeValue()
    {
        // Arrange & Act
        var money = Money.BRL(-50m);

        // Assert
        money.Amount.Should().Be(-50m);
    }

    [Fact]
    public void Add_EachOperandRoundedIndividuallyBeforeAdding()
    {
        // Arrange — cada Money arredonda ao ser criado, entao 1.005 vira 1.00 (MidpointRounding.AwayFromZero)
        // Math.Round(1.005, 2) = 1.00 em .NET por padrao (bankers rounding)
        var roundedInput = Math.Round(1.005m, 2);
        var a = Money.BRL(1.005m);
        var b = Money.BRL(1.005m);

        // Act
        var result = a.Add(b);

        // Assert
        result.Amount.Should().Be(roundedInput + roundedInput);
    }
}
