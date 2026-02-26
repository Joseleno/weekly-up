using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeeklyUp.Api.Tests.Infrastructure;
using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Infrastructure.Persistence;

namespace WeeklyUp.Api.Tests.E2E;

[Collection("E2E")]
public sealed class LoginE2ETests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;

    public LoginE2ETests(DatabaseFixture fixture)
        => _fixture = fixture;

    public Task InitializeAsync() => _fixture.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Login_ComExternalAuthIdValido_RetornaToken()
    {
        // Arrange — registra um usuário para ter dados no banco
        const string externalAuthId = "google-oauth2|123456789";
        const string email = "joao@exemplo.com";
        await RegisterUserAsync(email, externalAuthId);

        var request = new { ExternalAuthId = externalAuthId, Email = email };

        // Act
        HttpResponseMessage response = await _fixture.Client
            .PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        AuthTokenDto? body = await response.Content.ReadFromJsonAsync<AuthTokenDto>();
        body.Should().NotBeNull();
        body!.Token.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBeNullOrWhiteSpace();
        body.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Login_ComEmailValido_QuandoExternalAuthIdNaoExiste_RetornaToken()
    {
        // Arrange — usuário sem ExternalAuthId
        const string email = "maria@exemplo.com";
        await RegisterUserAsync(email, externalAuthId: null);

        var request = new { ExternalAuthId = "nao-existe-id", Email = email };

        // Act
        HttpResponseMessage response = await _fixture.Client
            .PostAsJsonAsync("/api/auth/login", request);

        // Assert — encontrou pelo email como fallback
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        AuthTokenDto? body = await response.Content.ReadFromJsonAsync<AuthTokenDto>();
        body!.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_ComCredenciaisInexistentes_Retorna404()
    {
        // Arrange — banco limpo, nenhum usuário cadastrado
        var request = new { ExternalAuthId = "nao-existe", Email = "naoexiste@exemplo.com" };

        // Act
        HttpResponseMessage response = await _fixture.Client
            .PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Login_ComEmailInvalido_Retorna422()
    {
        // Arrange
        var request = new { ExternalAuthId = "qualquer-id", Email = "nao-e-um-email" };

        // Act
        HttpResponseMessage response = await _fixture.Client
            .PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Login_ComExternalAuthIdVazio_Retorna422()
    {
        // Arrange
        var request = new { ExternalAuthId = "", Email = "valido@exemplo.com" };

        // Act
        HttpResponseMessage response = await _fixture.Client
            .PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    private async Task RegisterUserAsync(string email, string? externalAuthId)
    {
        var request = new
        {
            Email = email,
            Name = "Usuário Teste",
            BusinessName = "Loja Teste",
            BusinessType = BusinessType.Ecommerce,
            ExternalAuthId = externalAuthId,
        };

        HttpResponseMessage response = await _fixture.Client
            .PostAsJsonAsync("/api/users", request);

        response.EnsureSuccessStatusCode();
    }
}
