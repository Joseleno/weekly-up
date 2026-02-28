// CA2012 is suppressed because NSubstitute's .Returns() setup uses ValueTask returned by
// .Send() intercept without awaiting it, which is intentional in test arrange blocks.
#pragma warning disable CA2012
using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Mediator;

using NSubstitute;

using WeeklyUp.Api.Tests.Infrastructure;
using WeeklyUp.Application.Metrics.Commands.AddManualMetric;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Api.Tests.Modules;

[Collection("Modules")]
public sealed class MetricsModuleTests
{
    private readonly ApiTestFixture _fixture;
    private readonly HttpClient _anonClient;
    private readonly HttpClient _authClient;

    public MetricsModuleTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
        _anonClient = fixture.CreateAnonymousClient();
        _authClient = fixture.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task AddManualMetric_SemAutenticacao_Retorna401()
    {
        var body = new
        {
            WeekStart = new DateOnly(2025, 1, 6),
            Revenue = (decimal?)1000m,
            SalesCount = (int?)10,
            NewCustomers = (int?)5,
            Visits = (int?)200,
        };

        HttpResponseMessage response = await _anonClient.PostAsJsonAsync("/api/metrics", body);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddManualMetric_QuandoAutenticado_Retorna204()
    {
        // Arrange
        _fixture.Mediator
            .Send(Arg.Any<AddManualMetricCommand>(), Arg.Any<CancellationToken>())
            .Returns((_) => ValueTask.FromResult(Result.Success(true)));

        var body = new
        {
            WeekStart = new DateOnly(2025, 1, 6),
            Revenue = (decimal?)1000m,
            SalesCount = (int?)10,
            NewCustomers = (int?)5,
            Visits = (int?)200,
        };

        // Act
        HttpResponseMessage response = await _authClient.PostAsJsonAsync("/api/metrics", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AddManualMetric_QuandoValidacaoFalha_Retorna422()
    {
        // Arrange
        var error = AppError.Validation("ManualMetric.WeekStartNotMonday", "A data deve ser segunda-feira.");
        _fixture.Mediator
            .Send(Arg.Any<AddManualMetricCommand>(), Arg.Any<CancellationToken>())
            .Returns((_) => ValueTask.FromResult(
                Result.Failure<bool>(error)));

        var body = new
        {
            WeekStart = new DateOnly(2025, 1, 7), // Tuesday
            Revenue = (decimal?)1000m,
            SalesCount = (int?)null,
            NewCustomers = (int?)null,
            Visits = (int?)null,
        };

        // Act
        HttpResponseMessage response = await _authClient.PostAsJsonAsync("/api/metrics", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task AddManualMetric_MediatorRecebeuComandoComUserId()
    {
        // Arrange
        _fixture.Mediator
            .Send(Arg.Any<AddManualMetricCommand>(), Arg.Any<CancellationToken>())
            .Returns((_) => ValueTask.FromResult(Result.Success(true)));

        var body = new
        {
            WeekStart = new DateOnly(2025, 1, 6),
            Revenue = (decimal?)500m,
            SalesCount = (int?)null,
            NewCustomers = (int?)null,
            Visits = (int?)null,
        };

        // Act
        await _authClient.PostAsJsonAsync("/api/metrics", body);

        // Assert — verifica que o UserId do ICurrentUserService foi passado ao command
        await _fixture.Mediator
            .Received()
            .Send(
                Arg.Is<AddManualMetricCommand>(c => c.UserId == ApiTestFixture.DefaultUserId),
                Arg.Any<CancellationToken>());
    }
}
