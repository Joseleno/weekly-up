using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.ValueObjects;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Infrastructure.AI;

public sealed class ClaudeInsightGenerator : IInsightGenerator
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly HttpClient _httpClient;
    private readonly ClaudeOptions _options;
    private readonly IDateTimeProvider _dateTime;
    private readonly ILogger<ClaudeInsightGenerator> _logger;

    public ClaudeInsightGenerator(
        IHttpClientFactory httpClientFactory,
        IOptions<ClaudeOptions> options,
        IDateTimeProvider dateTime,
        ILogger<ClaudeInsightGenerator> logger)
    {
        _httpClient = httpClientFactory.CreateClient("claude");
        _options = options.Value;
        _dateTime = dateTime;
        _logger = logger;
    }

    public async Task<Result<ReportInsights>> GenerateAsync(
        ReportMetrics metrics,
        BusinessType businessType,
        string language,
        CancellationToken ct = default)
    {
        try
        {
            var prompt = InsightPromptBuilder.Build(metrics, businessType, language);
            var responseText = await CallClaudeAsync(prompt, ct);
            return ParseInsights(responseText);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Falha HTTP ao chamar Claude — usando fallback");
            return BuildFallbackInsights(metrics);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Falha ao parsear resposta do Claude — usando fallback");
            return BuildFallbackInsights(metrics);
        }
    }

    private async Task<string> CallClaudeAsync(string prompt, CancellationToken ct)
    {
        var request = new
        {
            model = _options.Model,
            max_tokens = _options.MaxTokens,
            messages = new[] { new { role = "user", content = prompt } },
        };

        var response = await _httpClient.PostAsJsonAsync("/v1/messages", request, _jsonOptions, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(ct);
        return ExtractText(body);
    }

    private static string ExtractText(string body)
    {
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;
    }

    private Result<ReportInsights> ParseInsights(string json)
    {
        // Remove possível markdown code block que Claude às vezes adiciona
        var clean = json.Trim();
        if (clean.StartsWith("```", StringComparison.Ordinal))
        {
            var start = clean.IndexOf('{', StringComparison.Ordinal);
            var end = clean.LastIndexOf('}');
            if (start >= 0 && end > start)
            {
                clean = clean[start..(end + 1)];
            }
        }

        using var doc = JsonDocument.Parse(clean);
        var root = doc.RootElement;

        return new ReportInsights(
            highlight: root.GetProperty("highlight").GetString() ?? string.Empty,
            alert: root.GetProperty("alert").GetString() ?? string.Empty,
            tip: root.GetProperty("tip").GetString() ?? string.Empty,
            generatedAt: _dateTime.UtcNow);
    }

    private Result<ReportInsights> BuildFallbackInsights(ReportMetrics metrics)
    {
        _logger.LogInformation("Gerando insights de fallback baseados nas métricas");

        var highlight = metrics.SalesCount > 0
            ? $"Você realizou {metrics.SalesCount} vendas com receita de R$ {metrics.Revenue.Amount:N2} nesta semana."
            : "Seus dados foram coletados com sucesso. Aguarde a próxima semana para comparações.";

        var alert = metrics.PreviousRevenue is not null && metrics.Revenue.Amount < metrics.PreviousRevenue.Amount
            ? "A receita desta semana foi inferior à semana anterior. Analise os fatores que podem ter impactado as vendas."
            : "Monitore a taxa de conversão de visitas em vendas para identificar oportunidades de melhoria.";

        var tip = metrics.TotalVisits > 0 && metrics.SalesCount > 0
            ? $"Sua taxa de conversão está em {(decimal)metrics.SalesCount / metrics.TotalVisits * 100:F1}%. Teste novas chamadas para ação nas páginas mais visitadas."
            : "Conecte mais integrações para receber insights mais precisos e personalizados.";

        return new ReportInsights(
            highlight: highlight,
            alert: alert,
            tip: tip,
            generatedAt: _dateTime.UtcNow);
    }
}
