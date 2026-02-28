using Blazored.LocalStorage;

using NSubstitute;

using WeeklyUp.WebApp.Services;

namespace WeeklyUp.WebApp.Tests.Services;

public sealed class AuthServiceTests
{
    private readonly ILocalStorageService _localStorage = Substitute.For<ILocalStorageService>();
    private readonly IAuthStateNotifier _authStateNotifier = Substitute.For<IAuthStateNotifier>();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_localStorage, _authStateNotifier);
    }

    [Fact]
    public async Task GetTokenAsync_WhenNoToken_ReturnsNull()
    {
        // Arrange
        _localStorage.GetItemAsStringAsync(AuthConstants.TokenExpiryKey)
            .Returns((string?)null);
        _localStorage.GetItemAsStringAsync(AuthConstants.TokenKey)
            .Returns((string?)null);

        // Act
        var token = await _sut.GetTokenAsync();

        // Assert
        token.Should().BeNull();
    }

    [Fact]
    public async Task GetTokenAsync_WhenTokenValid_ReturnsToken()
    {
        // Arrange
        const string expectedToken = "valid.jwt.token";
        var futureExpiry = DateTimeOffset.UtcNow.AddHours(1).ToString("O");

        _localStorage.GetItemAsStringAsync(AuthConstants.TokenExpiryKey)
            .Returns(futureExpiry);
        _localStorage.GetItemAsStringAsync(AuthConstants.TokenKey)
            .Returns(expectedToken);

        // Act
        var token = await _sut.GetTokenAsync();

        // Assert
        token.Should().Be(expectedToken);
    }

    [Fact]
    public async Task GetTokenAsync_WhenTokenExpired_LogsOutAndReturnsNull()
    {
        // Arrange
        var pastExpiry = DateTimeOffset.UtcNow.AddHours(-1).ToString("O");

        _localStorage.GetItemAsStringAsync(AuthConstants.TokenExpiryKey)
            .Returns(pastExpiry);

        // Act
        var token = await _sut.GetTokenAsync();

        // Assert
        token.Should().BeNull();
        await _localStorage.Received(1)
            .RemoveItemAsync(AuthConstants.TokenKey);
        await _localStorage.Received(1)
            .RemoveItemAsync(AuthConstants.TokenExpiryKey);
        await _localStorage.DidNotReceive()
            .GetItemAsStringAsync(AuthConstants.TokenKey);
    }

    [Fact]
    public async Task SetTokenAsync_StoresTokenAndNotifiesAuthState()
    {
        // Arrange
        const string token = "new.jwt.token";
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);

        // Act
        await _sut.SetTokenAsync(token, expiresAt);

        // Assert
        await _localStorage.Received(1)
            .SetItemAsStringAsync(AuthConstants.TokenKey, token);
        await _localStorage.Received(1)
            .SetItemAsStringAsync(
                AuthConstants.TokenExpiryKey,
                expiresAt.ToString("O"));
        _authStateNotifier.Received(1).NotifyAuthStateChanged();
    }

    [Fact]
    public async Task LogoutAsync_RemovesTokensAndNotifiesAuthState()
    {
        // Act
        await _sut.LogoutAsync();

        // Assert
        await _localStorage.Received(1)
            .RemoveItemAsync(AuthConstants.TokenKey);
        await _localStorage.Received(1)
            .RemoveItemAsync(AuthConstants.TokenExpiryKey);
        _authStateNotifier.Received(1).NotifyAuthStateChanged();
    }
}
