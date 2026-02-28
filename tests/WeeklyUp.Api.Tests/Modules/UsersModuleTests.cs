// CA2012 is suppressed because NSubstitute's .Returns() setup uses ValueTask returned by
// .Send() intercept without awaiting it, which is intentional in test arrange blocks.
#pragma warning disable CA2012
using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Mediator;

using NSubstitute;

using WeeklyUp.Api.Tests.Infrastructure;
using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Application.Users.Commands.UpdateUserProfile;
using WeeklyUp.Application.Users.Commands.UpgradePlan;
using WeeklyUp.Application.Users.Commands.VerifyEmail;
using WeeklyUp.Application.Users.Queries.GetUserDashboard;
using WeeklyUp.Application.Users.Queries.GetUserProfile;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Api.Tests.Modules;

[Collection("Modules")]
public sealed class UsersModuleTests
{
    private readonly ApiTestFixture _fixture;
    private readonly HttpClient _anonClient;
    private readonly HttpClient _authClient;

    public UsersModuleTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
        _anonClient = fixture.CreateAnonymousClient();
        _authClient = fixture.CreateAuthenticatedClient();
    }

    // ─── POST /api/users/verify ───────────────────────────────────────────────

    [Fact]
    public async Task VerifyEmail_QuandoValido_Retorna204()
    {
        // Arrange
        _fixture.Mediator
            .Send(Arg.Any<VerifyEmailCommand>(), Arg.Any<CancellationToken>())
            .Returns((_) => ValueTask.FromResult(Result.Success(true)));

        var body = new { UserId = Guid.NewGuid(), Token = "abc123" };

        // Act
        HttpResponseMessage response = await _anonClient.PostAsJsonAsync("/api/users/verify", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task VerifyEmail_QuandoTokenInvalido_Retorna422()
    {
        // Arrange
        var error = AppError.Validation("User.InvalidToken", "Token inválido.");
        _fixture.Mediator
            .Send(Arg.Any<VerifyEmailCommand>(), Arg.Any<CancellationToken>())
            .Returns((_) => ValueTask.FromResult(Result.Failure<bool>(error)));

        var body = new { UserId = Guid.NewGuid(), Token = "wrong" };

        // Act
        HttpResponseMessage response = await _anonClient.PostAsJsonAsync("/api/users/verify", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ─── PUT /api/users/profile ───────────────────────────────────────────────

    [Fact]
    public async Task UpdateProfile_SemAutenticacao_Retorna401()
    {
        // Arrange
        var body = new { Name = "Novo Nome", BusinessName = "Loja", BusinessType = BusinessType.Services };

        // Act — sem Authorization header
        HttpResponseMessage response = await _anonClient.PutAsJsonAsync("/api/users/profile", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProfile_QuandoAutenticado_Retorna200()
    {
        // Arrange
        var profile = new UserProfileDto(
            ApiTestFixture.DefaultUserId, "user@test.com", "Novo Nome", "Loja",
            "Services", "Free", true, true, DateTimeOffset.UtcNow);

        _fixture.Mediator
            .Send(Arg.Any<UpdateUserProfileCommand>(), Arg.Any<CancellationToken>())
            .Returns((_) => ValueTask.FromResult(Result.Success(profile)));

        var body = new { Name = "Novo Nome", BusinessName = "Loja", BusinessType = BusinessType.Services };

        // Act
        HttpResponseMessage response = await _authClient.PutAsJsonAsync("/api/users/profile", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── PUT /api/users/plan ──────────────────────────────────────────────────

    [Fact]
    public async Task UpgradePlan_SemAutenticacao_Retorna401()
    {
        // Act
        HttpResponseMessage response = await _anonClient.PutAsJsonAsync(
            "/api/users/plan", new { NewPlan = PlanType.Pro });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpgradePlan_QuandoAutenticado_Retorna204()
    {
        // Arrange
        _fixture.Mediator
            .Send(Arg.Any<UpgradePlanCommand>(), Arg.Any<CancellationToken>())
            .Returns((_) => ValueTask.FromResult(Result.Success(true)));

        // Act
        HttpResponseMessage response = await _authClient.PutAsJsonAsync(
            "/api/users/plan", new { NewPlan = PlanType.Pro });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ─── GET /api/users/me ────────────────────────────────────────────────────

    [Fact]
    public async Task GetProfile_SemAutenticacao_Retorna401()
    {
        HttpResponseMessage response = await _anonClient.GetAsync("/api/users/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProfile_QuandoAutenticado_Retorna200()
    {
        // Arrange
        var profile = new UserProfileDto(
            ApiTestFixture.DefaultUserId, "user@test.com", "Nome", "Loja",
            "Ecommerce", "Free", true, true, DateTimeOffset.UtcNow);

        _fixture.Mediator
            .Send(Arg.Any<GetUserProfileQuery>(), Arg.Any<CancellationToken>())
            .Returns((_) => ValueTask.FromResult(Result.Success(profile)));

        // Act
        HttpResponseMessage response = await _authClient.GetAsync("/api/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── GET /api/users/dashboard ─────────────────────────────────────────────

    [Fact]
    public async Task GetDashboard_SemAutenticacao_Retorna401()
    {
        HttpResponseMessage response = await _anonClient.GetAsync("/api/users/dashboard");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDashboard_QuandoAutenticado_Retorna200()
    {
        // Arrange
        var profile = new UserProfileDto(
            ApiTestFixture.DefaultUserId, "user@test.com", "Nome", "Loja",
            "Ecommerce", "Free", true, true, DateTimeOffset.UtcNow);

        var dashboard = new UserDashboardDto(profile, null, [], false);

        _fixture.Mediator
            .Send(Arg.Any<GetUserDashboardQuery>(), Arg.Any<CancellationToken>())
            .Returns((_) => ValueTask.FromResult(Result.Success(dashboard)));

        // Act
        HttpResponseMessage response = await _authClient.GetAsync("/api/users/dashboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
