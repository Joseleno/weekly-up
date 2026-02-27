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
using WeeklyUp.Application.Reports.Commands.GenerateWeeklyReport;
using WeeklyUp.Application.Reports.Commands.UpdateReportPreferences;
using WeeklyUp.Application.Reports.Queries.GetReportDetail;
using WeeklyUp.Application.Reports.Queries.GetReportHistory;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Api.Tests.Modules;

[Collection("Modules")]
public sealed class ReportsModuleTests
{
    private readonly ApiTestFixture _fixture;
    private readonly HttpClient _anonClient;
    private readonly HttpClient _authClient;

    public ReportsModuleTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
        _anonClient = fixture.CreateAnonymousClient();
        _authClient = fixture.CreateAuthenticatedClient();
    }

    // ─── GET /api/reports ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetReportHistory_SemAutenticacao_Retorna401()
    {
        HttpResponseMessage response = await _anonClient.GetAsync("/api/reports?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetReportHistory_QuandoAutenticado_Retorna200()
    {
        // Arrange
        var paged = new PagedListDto<ReportSummaryDto>([], 1, 10, 0, false, false);

        _fixture.Mediator
            .Send(Arg.Any<GetReportHistoryQuery>(), Arg.Any<CancellationToken>())
            .Returns((_) => ValueTask.FromResult(
                Result.Success(paged)));

        // Act
        HttpResponseMessage response = await _authClient.GetAsync("/api/reports?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── GET /api/reports/{id} ────────────────────────────────────────────────

    [Fact]
    public async Task GetReportDetail_SemAutenticacao_Retorna401()
    {
        HttpResponseMessage response = await _anonClient.GetAsync("/api/reports/" + Guid.NewGuid());
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetReportDetail_QuandoAutenticado_Retorna200()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var dto = new ReportDetailDto(
            reportId, "01/01/2025", "Sent", null, null, null, DateTimeOffset.UtcNow);

        _fixture.Mediator
            .Send(Arg.Any<GetReportDetailQuery>(), Arg.Any<CancellationToken>())
            .Returns((_) => ValueTask.FromResult(Result.Success(dto)));

        // Act
        HttpResponseMessage response = await _authClient.GetAsync("/api/reports/" + reportId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetReportDetail_QuandoNaoEncontrado_Retorna404()
    {
        // Arrange
        var error = AppError.NotFound("Report.NotFound", "Relatório não encontrado.");
        _fixture.Mediator
            .Send(Arg.Any<GetReportDetailQuery>(), Arg.Any<CancellationToken>())
            .Returns((_) => ValueTask.FromResult(
                Result.Failure<ReportDetailDto>(error)));

        // Act
        HttpResponseMessage response = await _authClient.GetAsync("/api/reports/" + Guid.NewGuid());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─── PUT /api/reports/preferences ────────────────────────────────────────

    [Fact]
    public async Task UpdatePreferences_SemAutenticacao_Retorna401()
    {
        var body = new
        {
            SendDay = DayOfWeekPreference.Monday,
            SendTime = new TimeOnly(7, 0),
            EnabledSections = new[] { "Metrics" },
        };
        HttpResponseMessage response = await _anonClient.PutAsJsonAsync("/api/reports/preferences", body);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdatePreferences_QuandoAutenticado_Retorna204()
    {
        // Arrange
        _fixture.Mediator
            .Send(Arg.Any<UpdateReportPreferencesCommand>(), Arg.Any<CancellationToken>())
            .Returns((_) => ValueTask.FromResult(Result.Success(true)));

        var body = new
        {
            SendDay = DayOfWeekPreference.Monday,
            SendTime = new TimeOnly(7, 0),
            EnabledSections = new[] { "Metrics", "Insights" },
        };

        // Act
        HttpResponseMessage response = await _authClient.PutAsJsonAsync("/api/reports/preferences", body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ─── POST /api/reports/generate ──────────────────────────────────────────

    [Fact]
    public async Task GenerateReport_SemAutenticacao_Retorna401()
    {
        HttpResponseMessage response = await _anonClient.PostAsJsonAsync("/api/reports/generate", new { });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GenerateReport_QuandoAutenticado_Retorna202()
    {
        // Arrange
        _fixture.Mediator
            .Send(Arg.Any<GenerateWeeklyReportCommand>(), Arg.Any<CancellationToken>())
            .Returns((_) => ValueTask.FromResult(Result.Success(true)));

        // Act
        HttpResponseMessage response = await _authClient.PostAsJsonAsync("/api/reports/generate", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task GenerateReport_MediatorRecebeuComandoComUserId()
    {
        // Arrange
        _fixture.Mediator
            .Send(Arg.Any<GenerateWeeklyReportCommand>(), Arg.Any<CancellationToken>())
            .Returns((_) => ValueTask.FromResult(Result.Success(true)));

        // Act
        await _authClient.PostAsJsonAsync("/api/reports/generate", new { });

        // Assert — verifica que o UserId correto foi passado ao command
        await _fixture.Mediator
            .Received()
            .Send(
                Arg.Is<GenerateWeeklyReportCommand>(c => c.UserId == ApiTestFixture.DefaultUserId),
                Arg.Any<CancellationToken>());
    }
}
