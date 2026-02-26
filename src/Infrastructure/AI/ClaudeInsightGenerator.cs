using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WeeklyUp.Application.Common.Interfaces;
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
        ReportMetrics metrics, string language, CancellationToken ct = default)
    {
        try
        {
            var prompt = BuildPrompt(metrics, language);
            var responseText = await CallClaudeApiAsync(prompt, ct);
            return ParseInsights(responseText);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Falha na chamada HTTP ao Claude");
            return AppError.Failure("Claude.HttpError", ex.Message);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Falha ao parsear resposta do Claude");
            return AppError.Failure("Claude.ParseError", ex.Message);
        }
    }

    private static string BuildPrompt(ReportMetrics metrics, string language) =>
        $"Analise estas métricas semanais de negócio e forneça: 1) Um destaque positivo, " +
        $"2) Um alerta/preocupação, 3) Uma dica de melhoria. " +
        $"Receita: {metrics.Revenue}, Vendas: {metrics.SalesCount}, Visitas: {metrics.TotalVisits}. " +
        $"Responda em {language} com formato JSON: {{\"highlight\":\"...\",\"alert\":\"...\",\"tip\":\"...\"}}";

    private async Task<string> CallClaudeApiAsync(string prompt, CancellationToken ct)
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
        return ExtractTextFromResponse(body);
    }

    private static string ExtractTextFromResponse(string body)
    {
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;
    }

    private Result<ReportInsights> ParseInsights(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        return new ReportInsights(
            highlight: root.GetProperty("highlight").GetString() ?? string.Empty,
            alert: root.GetProperty("alert").GetString() ?? string.Empty,
            tip: root.GetProperty("tip").GetString() ?? string.Empty,
            generatedAt: _dateTime.UtcNow);
    }
}
