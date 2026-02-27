using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Interfaces.Services;
using WeeklyUp.Infrastructure.Email.Templates;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Infrastructure.Email;

public sealed class ResendEmailSender : IEmailSender
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly HttpClient _httpClient;
    private readonly ResendOptions _options;
    private readonly ILogger<ResendEmailSender> _logger;

    public ResendEmailSender(
        IHttpClientFactory httpClientFactory,
        IOptions<ResendOptions> options,
        ILogger<ResendEmailSender> logger)
    {
        _httpClient = httpClientFactory.CreateClient("resend");
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result<bool>> SendWeeklyReportAsync(
        string recipientEmail, string recipientName, Report report, CancellationToken cancellationToken = default)
    {
        var html = WeeklyReportEmailTemplate.Build(recipientName, report);
        var subject = $"Seu Relatorio Semanal - {report.WeekRange}";
        return await SendEmailAsync(recipientEmail, subject, html, cancellationToken);
    }

    public async Task<Result<bool>> SendWelcomeAsync(
        string recipientEmail, string recipientName, CancellationToken cancellationToken = default)
    {
        var html = WelcomeEmailTemplate.Build(recipientName);
        return await SendEmailAsync(recipientEmail, "Bem-vindo ao WeeklyUp!", html, cancellationToken);
    }

    public async Task<Result<bool>> SendVerificationEmailAsync(
        string recipientEmail, string recipientName, string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return AppError.Validation("Email.InvalidToken", "Token de verificacao nao pode ser vazio.");
        }

        var html = VerificationEmailTemplate.Build(recipientName, token);
        return await SendEmailAsync(recipientEmail, "Confirme seu email - WeeklyUp", html, cancellationToken);
    }

    private async Task<Result<bool>> SendEmailAsync(
        string to, string subject, string html, CancellationToken ct)
    {
        try
        {
            var payload = new
            {
                from = $"{_options.FromName} <{_options.FromEmail}>",
                to = new[] { to },
                subject,
                html,
            };
            var response = await _httpClient.PostAsJsonAsync("/emails", payload, _jsonOptions, ct);
            return response.IsSuccessStatusCode
                ? Result.Success(true)
                : AppError.Failure("Email.SendFailed", $"Resend retornou {response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Falha ao enviar email para {Email}", to);
            return AppError.Failure("Email.Exception", ex.Message);
        }
    }
}
