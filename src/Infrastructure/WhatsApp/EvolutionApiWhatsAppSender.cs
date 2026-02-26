using System.Net.Http.Json;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Interfaces.Services;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Infrastructure.WhatsApp;

public sealed class EvolutionApiWhatsAppSender : IWhatsAppSender
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly EvolutionApiOptions _options;
    private readonly ILogger<EvolutionApiWhatsAppSender> _logger;

    public EvolutionApiWhatsAppSender(
        IHttpClientFactory httpClientFactory,
        IOptions<EvolutionApiOptions> options,
        ILogger<EvolutionApiWhatsAppSender> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result<bool>> SendWeeklyReportAsync(
        string phoneNumber, string recipientName, Report report, CancellationToken ct = default)
    {
        try
        {
            string text = WhatsAppMessageBuilder.Build(recipientName, report);
            return await SendTextAsync(phoneNumber, text, ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Falha ao enviar WhatsApp para {Phone}", phoneNumber);
            return AppError.Failure("WhatsApp.Exception", ex.Message);
        }
    }

    private async Task<Result<bool>> SendTextAsync(
        string phoneNumber, string text, CancellationToken ct)
    {
        string url = $"/message/sendText/{_options.Instance}";
        HttpClient client = _httpClientFactory.CreateClient("evolutionapi");

        var payload = new
        {
            number = FormatPhoneNumber(phoneNumber),
            text,
        };

        HttpResponseMessage response = await client.PostAsJsonAsync(url, payload, ct);
        return response.IsSuccessStatusCode
            ? Result.Success(true)
            : AppError.Failure("WhatsApp.SendFailed", $"Evolution API retornou {response.StatusCode}");
    }

    private static string FormatPhoneNumber(string phone)
    {
        phone = phone.Trim().TrimStart('+').Replace("-", "").Replace(" ", "");
        if (!phone.StartsWith("55", StringComparison.Ordinal))
        {
            phone = "55" + phone.TrimStart('0');
        }

        return phone;
    }
}
