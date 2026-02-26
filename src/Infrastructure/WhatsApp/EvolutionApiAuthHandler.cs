using Microsoft.Extensions.Options;

namespace WeeklyUp.Infrastructure.WhatsApp;

public sealed class EvolutionApiAuthHandler : DelegatingHandler
{
    private readonly string _apiKey;

    public EvolutionApiAuthHandler(IOptions<EvolutionApiOptions> options)
        => _apiKey = options.Value.ApiKey;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Add("apikey", _apiKey);
        return base.SendAsync(request, cancellationToken);
    }
}
