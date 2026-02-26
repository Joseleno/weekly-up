using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;

namespace WeeklyUp.Infrastructure.WhatsApp;

public sealed class TwilioAuthHandler : DelegatingHandler
{
    private readonly string _credentials;

    public TwilioAuthHandler(IOptions<TwilioOptions> options)
    {
        var opts = options.Value;
        _credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{opts.AccountSid}:{opts.AuthToken}"));
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", _credentials);
        return base.SendAsync(request, cancellationToken);
    }
}
