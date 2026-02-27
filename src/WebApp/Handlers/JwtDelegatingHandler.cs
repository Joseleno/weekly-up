using System.Net;
using System.Net.Http.Headers;

using WeeklyUp.WebApp.Services;

namespace WeeklyUp.WebApp.Handlers;

public sealed class JwtDelegatingHandler(AuthService authService) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await authService.GetTokenAsync();

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await authService.LogoutAsync();
        }

        return response;
    }
}
