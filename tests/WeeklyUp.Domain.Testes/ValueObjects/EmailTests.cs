using FluentAssertions;
using WeeklyUp.Domain.ValueObjects;

namespace WeeklyUp.Domain.Testes.ValueObjects;

public sealed class EmailTests
{
    [Fact]
    public void Create_ValidEmail_ReturnsSuccess()
    {
        // Arrange
        const string email = "user@example.com";

        // Act
        var result = Email.Create(email);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("user@example.com");
    }

    [Fact]
    public void Create_ValidEmail_NormalizesToLowercase()
    {
        // Arrange
        const string email = "User@EXAMPLE.COM";

        // Act
        var result = Email.Create(email);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("user@example.com");
    }

    [Fact]
    public void Create_ValidEmail_TrimsWhitespace()
    {
        // Arrange
        const string email = "  user@example.com  ";

        // Act
        var result = Email.Create(email);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("user@example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_EmptyOrNull_ReturnsFailure(string? email)
    {
        // Act
        var result = Email.Create(email!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Email.Empty");
    }

    [Fact]
    public void Create_TooLong_ReturnsFailure()
    {
        // Arrange
        string email = new string('a', 250) + "@b.com"; // > 255 caracteres

        // Act
        var result = Email.Create(email);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Email.TooLong");
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing@domain")]
    [InlineData("@nodomain.com")]
    [InlineData("spaces in@email.com")]
    public void Create_InvalidFormat_ReturnsFailure(string email)
    {
        // Act
        var result = Email.Create(email);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Email.InvalidFormat");
    }

    [Fact]
    public void Equals_SameValue_ReturnsTrue()
    {
        // Arrange
        var email1 = Email.Create("user@example.com").Value;
        var email2 = Email.Create("user@example.com").Value;

        // Assert
        email1.Should().Be(email2);
    }

    [Fact]
    public void Equals_DifferentValue_ReturnsFalse()
    {
        // Arrange
        var email1 = Email.Create("user1@example.com").Value;
        var email2 = Email.Create("user2@example.com").Value;

        // Assert
        email1.Should().NotBe(email2);
    }

    [Fact]
    public void ToString_ReturnsEmailValue()
    {
        // Arrange
        var email = Email.Create("user@example.com").Value;

        // Assert
        email.ToString().Should().Be("user@example.com");
    }
}
