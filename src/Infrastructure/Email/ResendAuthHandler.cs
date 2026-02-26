using Microsoft.Extensions.Options;

namespace WeeklyUp.Infrastructure.Email;

public sealed class ResendAuthHandler : DelegatingHandler
{
    private readonly ResendOptions _options;

    public ResendAuthHandler(IOptions<ResendOptions> options)
    {
        _options = options.Value;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiKey);
        return base.SendAsync(request, cancellationToken);
    }
}
