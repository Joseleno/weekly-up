using FluentAssertions;
using WeeklyUp.Domain.ValueObjects;

namespace WeeklyUp.Domain.Testes.ValueObjects;

public sealed class BusinessNameTests
{
    [Fact]
    public void Create_ValidName_ReturnsSuccess()
    {
        // Arrange
        const string name = "Minha Loja";

        // Act
        var result = BusinessName.Create(name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("Minha Loja");
    }

    [Fact]
    public void Create_ValidNameWithWhitespace_Trims()
    {
        // Arrange
        const string name = "  Loja  ";

        // Act
        var result = BusinessName.Create(name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("Loja");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_EmptyOrNull_ReturnsFailure(string? name)
    {
        // Act
        var result = BusinessName.Create(name!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BusinessName.Empty");
    }

    [Fact]
    public void Create_SingleCharacterName_ReturnsFailure()
    {
        // Arrange
        const string name = "A";

        // Act
        var result = BusinessName.Create(name);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BusinessName.TooShort");
    }

    [Fact]
    public void Create_NameExceedingMaxLength_ReturnsFailure()
    {
        // Arrange
        string name = new string('A', 101); // 101 > limite de 100

        // Act
        var result = BusinessName.Create(name);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("BusinessName.TooLong");
    }

    [Fact]
    public void Create_ExactlyMaxLength_ReturnsSuccess()
    {
        // Arrange
        string name = new string('A', 100); // limite exato

        // Act
        var result = BusinessName.Create(name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Length.Should().Be(100);
    }

    [Fact]
    public void Create_ExactlyMinLength_ReturnsSuccess()
    {
        // Arrange
        const string name = "AB"; // 2 caracteres — limite minimo exato

        // Act
        var result = BusinessName.Create(name);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Equals_SameValue_ReturnsTrue()
    {
        // Arrange
        var bn1 = BusinessName.Create("Minha Loja").Value;
        var bn2 = BusinessName.Create("Minha Loja").Value;

        // Assert
        bn1.Should().Be(bn2);
    }

    [Fact]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        // Arrange
        var bn1 = BusinessName.Create("Loja A").Value;
        var bn2 = BusinessName.Create("Loja B").Value;

        // Assert
        bn1.Should().NotBe(bn2);
    }

    [Fact]
    public void ToString_ReturnsNameValue()
    {
        // Arrange
        var businessName = BusinessName.Create("Minha Loja").Value;

        // Assert
        businessName.ToString().Should().Be("Minha Loja");
    }
}
