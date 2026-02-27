using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using WeeklyUp.Api.Tests.Infrastructure;
using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Infrastructure.Persistence;

namespace WeeklyUp.Api.Tests.E2E;

[Collection("E2E")]
[Trait("Category", "E2E")]
public sealed class ReportGenerationE2ETests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;

    public ReportGenerationE2ETests(DatabaseFixture fixture)
        => _fixture = fixture;

    public Task InitializeAsync() => _fixture.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GenerateReport_SemAutenticacao_Retorna401()
    {
        // Act
        HttpResponseMessage response = await _fixture.Client
            .PostAsync("/api/reports/generate", content: null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GenerateReport_UsuarioAutenticado_CriaReportComStatusPending_Retorna202()
    {
        // Arrange
        string token = await RegisterAndLoginAsync("relatorio@exemplo.com", "google|rel001");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/reports/generate");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        HttpResponseMessage response = await _fixture.Client.SendAsync(request);

        // Assert — HTTP
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        // Assert — banco de dados
        using IServiceScope scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var report = db.Set<WeeklyUp.Domain.Entities.Report>()
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefault();

        report.Should().NotBeNull();
        report!.Status.Should().Be(ReportStatus.Pending);
    }

    [Fact]
    public async Task GenerateReport_SegundaChamadaNaSemana_Retorna409()
    {
        // Arrange
        string token = await RegisterAndLoginAsync("conflito@exemplo.com", "google|conf002");

        // Primeira chamada — deve criar o relatório
        using var firstRequest = new HttpRequestMessage(HttpMethod.Post, "/api/reports/generate");
        firstRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        HttpResponseMessage firstResponse = await _fixture.Client.SendAsync(firstRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

        // Act — segunda chamada na mesma semana
        using var secondRequest = new HttpRequestMessage(HttpMethod.Post, "/api/reports/generate");
        secondRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        HttpResponseMessage secondResponse = await _fixture.Client.SendAsync(secondRequest);

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    private async Task<string> RegisterAndLoginAsync(string email, string externalAuthId)
    {
        var registerRequest = new
        {
            Email = email,
            Name = "Usuário Relatório",
            BusinessName = "Empresa Teste",
            BusinessType = BusinessType.Ecommerce,
            ExternalAuthId = externalAuthId,
        };

        HttpResponseMessage registerResponse = await _fixture.Client
            .PostAsJsonAsync("/api/users", registerRequest);
        registerResponse.EnsureSuccessStatusCode();

        var loginRequest = new { ExternalAuthId = externalAuthId, Email = email };
        HttpResponseMessage loginResponse = await _fixture.Client
            .PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.EnsureSuccessStatusCode();

        AuthTokenDto? tokenDto = await loginResponse.Content.ReadFromJsonAsync<AuthTokenDto>();
        return tokenDto!.Token;
    }
}
