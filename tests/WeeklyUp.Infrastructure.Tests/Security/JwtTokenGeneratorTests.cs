using System.IdentityModel.Tokens.Jwt;

using FluentAssertions;

using Microsoft.Extensions.Options;

using NSubstitute;

using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.Interfaces.Services;
using WeeklyUp.Infrastructure.Security;
using WeeklyUp.Shared.Constants;

namespace WeeklyUp.Infrastructure.Tests.Security;

public sealed class JwtTokenGeneratorTests
{
    private const string ValidSecret = "super-secret-key-at-least-32-chars!!";
    private const string TestIssuer = "test-issuer";
    private const string TestAudience = "test-audience";
    private const int ExpirationMinutes = 60;

    private static JwtTokenGenerator CreateGenerator() =>
        new(Options.Create(new JwtOptions
        {
            Key = ValidSecret,
            Issuer = TestIssuer,
            Audience = TestAudience,
            ExpirationMinutes = ExpirationMinutes,
        }));

    private static User CreateUser()
    {
        var result = User.Create("user@example.com", "Test User", "My Business", BusinessType.Ecommerce);
        return result.Value;
    }

    [Fact]
    public void GenerateToken_ShouldReturnValidJwtToken()
    {
        // Arrange
        var generator = CreateGenerator();
        var user = CreateUser();

        // Act
        var token = generator.GenerateToken(user);

        // Assert
        token.Should().NotBeNullOrEmpty();

        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeTrue();

        var jwt = handler.ReadJwtToken(token);
        jwt.Issuer.Should().Be(TestIssuer);
        jwt.Audiences.Should().Contain(TestAudience);
    }

    [Fact]
    public void GenerateToken_ShouldIncludeCorrectClaims()
    {
        // Arrange
        var generator = CreateGenerator();
        var user = CreateUser();

        // Act
        var token = generator.GenerateToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        var subClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
        subClaim.Should().NotBeNull();
        subClaim!.Value.Should().Be(user.Id.ToString());

        var emailClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);
        emailClaim.Should().NotBeNull();
        emailClaim!.Value.Should().Be(user.Email.Value);

        var userIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == CustomClaimTypes.UserId);
        userIdClaim.Should().NotBeNull();
        userIdClaim!.Value.Should().Be(user.Id.ToString());
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnNonEmptyBase64String()
    {
        // Arrange
        var generator = CreateGenerator();

        // Act
        var refreshToken = generator.GenerateRefreshToken();

        // Assert
        refreshToken.Should().NotBeNullOrEmpty();

        var bytes = Convert.FromBase64String(refreshToken);
        bytes.Should().HaveCount(64);

        refreshToken.Length.Should().Be(88);
    }
}
