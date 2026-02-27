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
using WeeklyUp.Application.Integrations.Commands.ConnectIntegration;
using WeeklyUp.Application.Integrations.Commands.DisconnectIntegration;
using WeeklyUp.Application.Integrations.Queries.GetIntegrations;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Api.Tests.Modules;

[Collection("Modules")]
public sealed class IntegrationsModuleTests
{
    private readonly ApiTestFixture _fixture;
    private readonly HttpClient _anonClient;
    private readonly HttpClient _authClient;

    public IntegrationsModuleTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
        _anonClient = fixture.CreateAnonymousClient();
        _authClient = fixture.CreateAuthenticatedClient();
    }

    // ─── GET /api/integrations ────────────────────────────────────────────────

    [Fact]
    public async Task GetIntegrations_SemAutenticacao_Retorna401()
    {
        HttpResponseMessage response = await _anonClient.GetAsync("/api/integrations");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetIntegrations_QuandoAutenticado_Retorna200()
    {
        // Arrange
        var integrations = new List<IntegrationDto>
        {
            new(Guid.NewGuid(), "GoogleAnalytics", "Active", "account-1", DateTimeOffset.UtcNow),
        };

        _fixture.Mediator
            .Send(Arg.Any<GetIntegrationsQuery>(), Arg.Any<CancellationToken>())
            .Returns((_) => ValueTask.FromResult(
                Result.Success<IReadOnlyList<IntegrationDto>>(integrations)));

        // Act
        HttpResponseMessage response = await _authClient.GetAsync("/api/integrations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── POST /api/integrations ───────────────────────────────────────────────

    [Fact]
    public async Task ConnectIntegration_SemAutenticacao_Retorna401()
    {
        var body = new
        {
            Provider = IntegrationProvider.GoogleAnalytics4,
            AccessToken = "at",
            RefreshToken = "rt",
            AccountId = "acc",
            PropertyId = (string?)null,
        };

        HttpResponseMessage response = await _anonClient.PostAsJsonAsync("/api/integrations", body);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ConnectIntegration_QuandoAutenticado_Retorna201()
    {
        // Arrange
        var dto = new IntegrationDto(
            Guid.NewGuid(), "GoogleAnalytics", "Active", "acc-1", DateTimeOffset.UtcNow);

        _fixture.Mediator
            .Send(Arg.Any<ConnectIntegrationCommand>(), Arg.Any<CancellationToken>())
            .Returns((_) => ValueTask.FromResult(Result.Success(dto)));

        var body = new
        {
            Provider = IntegrationProvider.GoogleAnalytics4,
            AccessToken = "access-token",
            RefreshToken = "refresh-token",
            AccountId = "account-id",
            PropertyId = (string?)null,
        };

        // Act
        HttpResponseMessage response = await _authClient.PostAsJsonAsync("/api/integrations", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ConnectIntegration_QuandoConflito_Retorna409()
    {
        // Arrange
        var error = AppError.Conflict("Integration.AlreadyConnected", "Integração já conectada.");
        _fixture.Mediator
            .Send(Arg.Any<ConnectIntegrationCommand>(), Arg.Any<CancellationToken>())
            .Returns((_) => ValueTask.FromResult(
                Result.Failure<IntegrationDto>(error)));

        var body = new
        {
            Provider = IntegrationProvider.GoogleAnalytics4,
            AccessToken = "at",
            RefreshToken = "rt",
            AccountId = "acc",
            PropertyId = (string?)null,
        };

        // Act
        HttpResponseMessage response = await _authClient.PostAsJsonAsync("/api/integrations", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ─── DELETE /api/integrations/{provider} ─────────────────────────────────

    [Fact]
    public async Task DisconnectIntegration_SemAutenticacao_Retorna401()
    {
        HttpResponseMessage response = await _anonClient
            .DeleteAsync("/api/integrations/GoogleAnalytics4");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DisconnectIntegration_QuandoAutenticado_Retorna204()
    {
        // Arrange
        _fixture.Mediator
            .Send(Arg.Any<DisconnectIntegrationCommand>(), Arg.Any<CancellationToken>())
            .Returns((_) => ValueTask.FromResult(Result.Success(true)));

        // Act
        HttpResponseMessage response = await _authClient
            .DeleteAsync("/api/integrations/GoogleAnalytics4");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DisconnectIntegration_QuandoNaoEncontrado_Retorna404()
    {
        // Arrange
        var error = AppError.NotFound("Integration.NotFound", "Integração não encontrada.");
        _fixture.Mediator
            .Send(Arg.Any<DisconnectIntegrationCommand>(), Arg.Any<CancellationToken>())
            .Returns((_) => ValueTask.FromResult(Result.Failure<bool>(error)));

        // Act
        HttpResponseMessage response = await _authClient
            .DeleteAsync("/api/integrations/GoogleAnalytics4");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
