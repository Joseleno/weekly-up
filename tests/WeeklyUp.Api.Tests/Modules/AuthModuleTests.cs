// CA2012 is suppressed because NSubstitute's .Returns() setup uses ValueTask returned by
// .Send() intercept without awaiting it, which is intentional in test arrange blocks.
#pragma warning disable CA2012
using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Mediator;

using NSubstitute;

using WeeklyUp.Api.Tests.Infrastructure;
using WeeklyUp.Application.Auth.Commands.Login;
using WeeklyUp.Application.Auth.Commands.VerifyEmailByToken;
using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Application.Users.Commands.RegisterUser;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Api.Tests.Modules;

[Collection("Modules")]
public sealed class AuthModuleTests
{
    private readonly ApiTestFixture _fixture;
    private readonly HttpClient _client;

    public AuthModuleTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateAnonymousClient();
    }

    // ─── POST /api/auth/register ─────────────────────────────────────────────

    [Fact]
    public async Task Register_QuandoValido_Retorna201ComToken()
    {
        // Arrange
        var token = new AuthTokenDto("jwt-token", DateTimeOffset.UtcNow.AddHours(1));

        _fixture.Mediator
            .Send(Arg.Any<RegisterUserCommand>(), Arg.Any<CancellationToken>())
            .Returns((_) => ValueTask.FromResult(Result.Success(token)));

        var body = new
        {
            Email = "user@test.com",
            Password = "Senha@123",
            Name = "Nome",
            BusinessName = "Loja",
            BusinessType = BusinessType.Ecommerce,
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/register", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<AuthTokenDto>();
        result.Should().NotBeNull();
        result!.Token.Should().Be("jwt-token");
    }

    [Fact]
    public async Task Register_QuandoConflito_Retorna409()
    {
        // Arrange
        var error = AppError.Conflict("User.EmailAlreadyExists", "Email já cadastrado.");
        _fixture.Mediator
            .Send(Arg.Any<RegisterUserCommand>(), Arg.Any<CancellationToken>())
            .Returns((_) => ValueTask.FromResult(Result.Failure<AuthTokenDto>(error)));

        var body = new
        {
            Email = "duplicate@test.com",
            Password = "Senha@123",
            Name = "Nome",
            BusinessName = "Loja",
            BusinessType = BusinessType.Ecommerce,
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/register", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ─── POST /api/auth/login ────────────────────────────────────────────────

    [Fact]
    public async Task Login_QuandoMediatorRetornaToken_Retorna200ComToken()
    {
        // Arrange
        var expectedToken = new AuthTokenDto(
            Token: "jwt-token",
            ExpiresAt: DateTimeOffset.UtcNow.AddHours(1));

        _fixture.Mediator
            .Send(Arg.Any<LoginCommand>(), Arg.Any<CancellationToken>())
            .Returns((_) => ValueTask.FromResult(Result.Success(expectedToken)));

        var body = new { Email = "user@test.com", Password = "Senha@123" };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/login", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        AuthTokenDto? token = await response.Content.ReadFromJsonAsync<AuthTokenDto>();
        token.Should().NotBeNull();
        token!.Token.Should().Be("jwt-token");
    }

    [Fact]
    public async Task Login_QuandoMediatorRetornaNotFound_Retorna404()
    {
        // Arrange
        var error = AppError.NotFound("Auth.InvalidCredentials", "Credenciais inválidas.");
        _fixture.Mediator
            .Send(Arg.Any<LoginCommand>(), Arg.Any<CancellationToken>())
            .Returns((_) => ValueTask.FromResult(Result.Failure<AuthTokenDto>(error)));

        var body = new { Email = "nao@existe.com", Password = "senha123" };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/login", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Login_QuandoMediatorRetornaValidation_Retorna422()
    {
        // Arrange
        var error = AppError.Validation("Auth.InvalidEmail", "Email inválido.");
        _fixture.Mediator
            .Send(Arg.Any<LoginCommand>(), Arg.Any<CancellationToken>())
            .Returns((_) => ValueTask.FromResult(Result.Failure<AuthTokenDto>(error)));

        var body = new { Email = "invalido", Password = "qualquer" };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/login", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Login_SemAutenticacao_NaoRequereAutorizacao()
    {
        // Arrange — POST /api/auth/login é público
        _fixture.Mediator
            .Send(Arg.Any<LoginCommand>(), Arg.Any<CancellationToken>())
            .Returns((_) => ValueTask.FromResult(
                Result.Success(new AuthTokenDto("t", DateTimeOffset.UtcNow.AddHours(1)))));

        var body = new { Email = "user@test.com", Password = "Senha@123" };

        // Act — sem Authorization header
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/login", body);

        // Assert — não deve retornar 401
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    // ─── POST /api/auth/verify-email ─────────────────────────────────────────

    [Fact]
    public async Task VerifyEmail_QuandoValido_Retorna204()
    {
        // Arrange
        _fixture.Mediator
            .Send(Arg.Any<VerifyEmailByTokenCommand>(), Arg.Any<CancellationToken>())
            .Returns((_) => ValueTask.FromResult(Result.Success(true)));

        var body = new { Token = "abc123" };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/verify-email", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task VerifyEmail_QuandoTokenInvalido_Retorna404()
    {
        // Arrange
        var error = AppError.NotFound("Auth.InvalidToken", "Token inválido.");
        _fixture.Mediator
            .Send(Arg.Any<VerifyEmailByTokenCommand>(), Arg.Any<CancellationToken>())
            .Returns((_) => ValueTask.FromResult(Result.Failure<bool>(error)));

        var body = new { Token = "wrong" };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/verify-email", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
