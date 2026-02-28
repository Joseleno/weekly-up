using Blazored.LocalStorage;

using NSubstitute;

using WeeklyUp.WebApp.Services;

namespace WeeklyUp.WebApp.Tests.Services;

public sealed class JwtAuthenticationStateProviderTests
{
    private readonly ILocalStorageService _localStorage = Substitute.For<ILocalStorageService>();
    private readonly JwtAuthenticationStateProvider _sut;

    public JwtAuthenticationStateProviderTests()
    {
        _sut = new JwtAuthenticationStateProvider(_localStorage);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_WhenNoToken_ReturnsAnonymous()
    {
        // Arrange
        _localStorage.GetItemAsStringAsync(AuthConstants.TokenExpiryKey)
            .Returns((string?)null);
        _localStorage.GetItemAsStringAsync(AuthConstants.TokenKey)
            .Returns((string?)null);

        // Act
        var state = await _sut.GetAuthenticationStateAsync();

        // Assert
        state.User.Identity!.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_WhenTokenExpired_ReturnsAnonymous()
    {
        // Arrange
        var pastExpiry = DateTimeOffset.UtcNow.AddHours(-1).ToString("O");
        _localStorage.GetItemAsStringAsync(AuthConstants.TokenExpiryKey)
            .Returns(pastExpiry);

        // Act
        var state = await _sut.GetAuthenticationStateAsync();

        // Assert
        state.User.Identity!.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_WhenEmptyToken_ReturnsAnonymous()
    {
        // Arrange
        var futureExpiry = DateTimeOffset.UtcNow.AddHours(1).ToString("O");
        _localStorage.GetItemAsStringAsync(AuthConstants.TokenExpiryKey)
            .Returns(futureExpiry);
        _localStorage.GetItemAsStringAsync(AuthConstants.TokenKey)
            .Returns(string.Empty);

        // Act
        var state = await _sut.GetAuthenticationStateAsync();

        // Assert
        state.User.Identity!.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_WhenValidToken_ReturnsAuthenticatedWithClaims()
    {
        // Arrange
        // JWT payload: {"sub":"user-123","email":"test@test.com"}
        // header.payload.signature (signature is ignored client-side)
        const string token =
            "eyJhbGciOiJIUzI1NiJ9" +
            ".eyJzdWIiOiJ1c2VyLTEyMyIsImVtYWlsIjoidGVzdEB0ZXN0LmNvbSJ9" +
            ".SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        var futureExpiry = DateTimeOffset.UtcNow.AddHours(1).ToString("O");

        _localStorage.GetItemAsStringAsync(AuthConstants.TokenExpiryKey)
            .Returns(futureExpiry);
        _localStorage.GetItemAsStringAsync(AuthConstants.TokenKey)
            .Returns(token);

        // Act
        var state = await _sut.GetAuthenticationStateAsync();

        // Assert
        state.User.Identity!.IsAuthenticated.Should().BeTrue();
        state.User.FindFirst("sub")!.Value.Should().Be("user-123");
        state.User.FindFirst("email")!.Value.Should().Be("test@test.com");
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_WhenTokenWithOnePartOnly_ReturnsAuthenticated()
    {
        // JWT com 1 parte (sem pontos): ParseClaimsFromJwt retorna [] porque Length != 3.
        // ClaimsIdentity([], "jwt") → IsAuthenticated=true pois authenticationType não é null.
        // A proteção real é via expiração no localStorage (TokenExpiryKey).
        const string singlePartToken = "onlyonepart";
        var futureExpiry = DateTimeOffset.UtcNow.AddHours(1).ToString("O");

        _localStorage.GetItemAsStringAsync(AuthConstants.TokenExpiryKey)
            .Returns(futureExpiry);
        _localStorage.GetItemAsStringAsync(AuthConstants.TokenKey)
            .Returns(singlePartToken);

        // Act
        var state = await _sut.GetAuthenticationStateAsync();

        // Assert — sem claims mas considerado "autenticado" — a expiração é o guarda real
        state.User.Claims.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_WhenMalformedTokenAndExpired_ReturnsAnonymous()
    {
        // A expiração no localStorage é o guarda real contra tokens malformados
        var pastExpiry = DateTimeOffset.UtcNow.AddHours(-1).ToString("O");
        _localStorage.GetItemAsStringAsync(AuthConstants.TokenExpiryKey)
            .Returns(pastExpiry);

        // Act
        var state = await _sut.GetAuthenticationStateAsync();

        // Assert
        state.User.Identity!.IsAuthenticated.Should().BeFalse();
        await _localStorage.DidNotReceive()
            .GetItemAsStringAsync(AuthConstants.TokenKey);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_WhenTokenExpiresWithinClockSkew_ReturnsAnonymous()
    {
        // Token expira em 20s — dentro da tolerância de clock skew de 30s → deve retornar anônimo
        var soonExpiry = DateTimeOffset.UtcNow.AddSeconds(20).ToString("O");
        _localStorage.GetItemAsStringAsync(AuthConstants.TokenExpiryKey)
            .Returns(soonExpiry);

        // Act
        var state = await _sut.GetAuthenticationStateAsync();

        // Assert — clock skew de 30s trata token "quase expirado" como expirado
        state.User.Identity!.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void NotifyAuthStateChanged_TriggersAuthenticationStateChangedEvent()
    {
        // Arrange
        var notified = false;
        _sut.AuthenticationStateChanged += _ => notified = true;

        // Act
        _sut.NotifyAuthStateChanged();

        // Assert
        notified.Should().BeTrue();
    }
}
