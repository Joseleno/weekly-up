using System.Net.Http.Json;

using WeeklyUp.WebApp.Models;

namespace WeeklyUp.WebApp.Services;

public sealed class ApiClient(HttpClient httpClient) : IApiClient
{
    public async Task<AuthTokenResponse?> LoginAsync(
        LoginRequest request,
        CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/auth/login", request, ct);
        await EnsureSuccessOrThrowAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<AuthTokenResponse>(ct);
    }

    public async Task<AuthTokenResponse?> RegisterAsync(
        RegisterRequest request,
        CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/auth/register", request, ct);
        await EnsureSuccessOrThrowAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<AuthTokenResponse>(ct);
    }

    public async Task<UserProfileResponse?> GetProfileAsync(CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync("api/users/profile", ct);
        await EnsureSuccessOrThrowAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<UserProfileResponse>(ct);
    }

    public async Task<DashboardResponse?> GetDashboardAsync(CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync("api/users/dashboard", ct);
        await EnsureSuccessOrThrowAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<DashboardResponse>(ct);
    }

    public async Task<PagedListResponse<ReportSummaryResponse>?> GetReportHistoryAsync(
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var url = $"api/reports?page={page}&pageSize={pageSize}";
        var response = await httpClient.GetAsync(url, ct);
        await EnsureSuccessOrThrowAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<PagedListResponse<ReportSummaryResponse>>(ct);
    }

    public async Task<ReportDetailResponse?> GetReportDetailAsync(
        Guid reportId,
        CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"api/reports/{reportId}", ct);
        await EnsureSuccessOrThrowAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<ReportDetailResponse>(ct);
    }

    public async Task<List<IntegrationResponse>?> GetIntegrationsAsync(
        CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync("api/integrations", ct);
        await EnsureSuccessOrThrowAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<List<IntegrationResponse>>(ct);
    }

    public async Task<bool> VerifyEmailAsync(string token, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/auth/verify-email",
            new { Token = token },
            ct);

        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        // Token inválido ou expirado é resultado esperado (não erro de servidor)
        if (response.StatusCode is System.Net.HttpStatusCode.BadRequest
            or System.Net.HttpStatusCode.NotFound
            or System.Net.HttpStatusCode.UnprocessableEntity)
        {
            return false;
        }

        // Erros de servidor inesperados devem ser propagados
        await EnsureSuccessOrThrowAsync(response, ct);
        return false;
    }

    public async Task<CheckoutSessionResponse?> CreateCheckoutSessionAsync(
        string plan,
        CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/billing/checkout",
            new { Plan = plan },
            ct);
        await EnsureSuccessOrThrowAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<CheckoutSessionResponse>(ct);
    }

    public async Task<BillingPortalSessionResponse?> CreateBillingPortalSessionAsync(
        CancellationToken ct = default)
    {
        var response = await httpClient.PostAsync("api/billing/portal", null, ct);
        await EnsureSuccessOrThrowAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<BillingPortalSessionResponse>(ct);
    }

    public async Task UpdateProfileAsync(
        UpdateProfileRequest request,
        CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync("api/users/profile", request, ct);
        await EnsureSuccessOrThrowAsync(response, ct);
    }

    public async Task UpdateReportPreferencesAsync(
        UpdateReportPreferencesRequest request,
        CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync("api/reports/preferences", request, ct);
        await EnsureSuccessOrThrowAsync(response, ct);
    }

    private static async Task EnsureSuccessOrThrowAsync(
        HttpResponseMessage response,
        CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        throw new HttpRequestException(body, null, response.StatusCode);
    }
}
