using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WeeklyUp.Api.Tests.Infrastructure;
using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.Interfaces.Services;
using WeeklyUp.Domain.ValueObjects;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Api.Tests.E2E;

/// <summary>
/// Testes E2E para envio de WhatsApp via Evolution API.
/// REQUER: Evolution API rodando em http://localhost:8080
///         com instância "weeklyup" conectada (QR Code escaneado).
/// </summary>
[Collection("E2E")]
[Trait("Category", "WhatsApp")]
public sealed class WhatsAppE2ETests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;

    // Substitua pelo seu número real para receber a mensagem de teste
    private const string TestPhoneNumber = "5511999999999";

    public WhatsAppE2ETests(DatabaseFixture fixture)
        => _fixture = fixture;

    public Task InitializeAsync() => _fixture.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task EnviarMensagem_ComDadosValidos_RetornaSuccesso()
    {
        // Arrange — busca o sender direto via DI (sem passar pela API HTTP)
        using IServiceScope scope = _fixture.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<IWhatsAppSender>();

        Report report = CriarReportFake();

        // Act
        Result<bool> result = await sender.SendWeeklyReportAsync(
            TestPhoneNumber, "Usuário Teste", report);

        // Assert
        result.IsSuccess.Should().BeTrue(
            because: "a Evolution API deve estar rodando e a instância conectada");
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task EnviarMensagem_ComNumeroInvalido_RetornaFalha()
    {
        // Arrange
        using IServiceScope scope = _fixture.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<IWhatsAppSender>();

        Report report = CriarReportFake();

        // Act — número claramente inválido
        Result<bool> result = await sender.SendWeeklyReportAsync(
            "0000000", "Teste", report);

        // Assert — a API deve retornar erro
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task EnviarRelatorio_ViaEndpointGenerateReport_DisparaWhatsApp()
    {
        // Arrange — registra usuário e faz login para obter token
        const string externalAuthId = "google|whatsapp-test-user";
        const string email = "whatsapp-test@exemplo.com";

        await RegistrarUsuarioAsync(email, externalAuthId);
        string token = await ObterTokenAsync(externalAuthId, email);

        _fixture.Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Act — dispara geração de relatório (que por sua vez enviaria WhatsApp)
        HttpResponseMessage response = await _fixture.Client
            .PostAsJsonAsync("/api/reports/generate", new { });

        // Assert — aceita o job em background
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    private static Report CriarReportFake()
    {
        Result<DateRange> weekRangeResult = DateRange.Create(
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)),
            DateOnly.FromDateTime(DateTime.UtcNow));

        return Report.Create(Guid.NewGuid(), weekRangeResult.Value);
    }

    private async Task RegistrarUsuarioAsync(string email, string externalAuthId)
    {
        var request = new
        {
            Email = email,
            Name = "Usuário WhatsApp Teste",
            BusinessName = "Loja WhatsApp",
            BusinessType = BusinessType.Ecommerce,
            ExternalAuthId = externalAuthId,
        };

        (await _fixture.Client.PostAsJsonAsync("/api/users", request))
            .EnsureSuccessStatusCode();
    }

    private async Task<string> ObterTokenAsync(string externalAuthId, string email)
    {
        var request = new { ExternalAuthId = externalAuthId, Email = email };

        HttpResponseMessage response = await _fixture.Client
            .PostAsJsonAsync("/api/auth/login", request);

        response.EnsureSuccessStatusCode();

        AuthTokenDto? body = await response.Content.ReadFromJsonAsync<AuthTokenDto>();
        return body!.Token;
    }
}
