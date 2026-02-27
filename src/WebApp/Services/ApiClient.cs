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
        return await httpClient.GetFromJsonAsync<UserProfileResponse>("api/users/profile", ct);
    }

    public async Task<DashboardResponse?> GetDashboardAsync(CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<DashboardResponse>("api/users/dashboard", ct);
    }

    public async Task<PagedListResponse<ReportSummaryResponse>?> GetReportHistoryAsync(
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var url = $"api/reports?page={page}&pageSize={pageSize}";
        return await httpClient.GetFromJsonAsync<PagedListResponse<ReportSummaryResponse>>(url, ct);
    }

    public async Task<ReportDetailResponse?> GetReportDetailAsync(
        Guid reportId,
        CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<ReportDetailResponse>($"api/reports/{reportId}", ct);
    }

    public async Task<List<IntegrationResponse>?> GetIntegrationsAsync(
        CancellationToken ct = default)
    {
        return await httpClient.GetFromJsonAsync<List<IntegrationResponse>>("api/integrations", ct);
    }

    public async Task<bool> VerifyEmailAsync(string token, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/auth/verify-email",
            new { Token = token },
            ct);
        return response.IsSuccessStatusCode;
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
