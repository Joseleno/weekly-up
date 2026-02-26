using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Interfaces.Services;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Infrastructure.WhatsApp;

public sealed class TwilioWhatsAppSender : IWhatsAppSender
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TwilioOptions _options;
    private readonly ILogger<TwilioWhatsAppSender> _logger;

    public TwilioWhatsAppSender(
        IHttpClientFactory httpClientFactory,
        IOptions<TwilioOptions> options,
        ILogger<TwilioWhatsAppSender> logger)
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
            string body = WhatsAppMessageBuilder.Build(recipientName, report);
            return await SendMessageAsync(phoneNumber, body, ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Falha ao enviar WhatsApp para {Phone}", phoneNumber);
            return AppError.Failure("WhatsApp.Exception", ex.Message);
        }
    }

    private async Task<Result<bool>> SendMessageAsync(
        string phoneNumber, string body, CancellationToken ct)
    {
        string formattedPhone = FormatPhoneNumber(phoneNumber);
        string url = $"/2010-04-01/Accounts/{_options.AccountSid}/Messages.json";

        HttpClient client = _httpClientFactory.CreateClient("twilio");

        using var formData = new FormUrlEncodedContent(
        [
            new("From", $"whatsapp:{_options.FromNumber}"),
            new("To", $"whatsapp:{formattedPhone}"),
            new("Body", body),
        ]);

        HttpResponseMessage response = await client.PostAsync(url, formData, ct);
        return response.IsSuccessStatusCode
            ? Result.Success(true)
            : AppError.Failure("WhatsApp.SendFailed", $"Twilio retornou {response.StatusCode}");
    }

    private static string FormatPhoneNumber(string phone)
    {
        phone = phone.Trim();
        if (!phone.StartsWith('+'))
        {
            phone = "+55" + phone.TrimStart('0');
        }

        return phone;
    }
}
