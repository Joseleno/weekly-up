using Microsoft.Extensions.Options;

namespace WeeklyUp.Infrastructure.AI;

public sealed class ClaudeAuthHandler : DelegatingHandler
{
    private readonly ClaudeOptions _options;

    public ClaudeAuthHandler(IOptions<ClaudeOptions> options)
    {
        _options = options.Value;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.TryAddWithoutValidation("x-api-key", _options.ApiKey);
        request.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");
        return base.SendAsync(request, cancellationToken);
    }
}
