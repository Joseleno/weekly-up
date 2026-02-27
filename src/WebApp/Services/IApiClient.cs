using WeeklyUp.WebApp.Models;

namespace WeeklyUp.WebApp.Services;

public interface IApiClient
{
    public Task<AuthTokenResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default);

    public Task<AuthTokenResponse?> RegisterAsync(RegisterRequest request, CancellationToken ct = default);

    public Task<UserProfileResponse?> GetProfileAsync(CancellationToken ct = default);

    public Task<DashboardResponse?> GetDashboardAsync(CancellationToken ct = default);

    public Task<PagedListResponse<ReportSummaryResponse>?> GetReportHistoryAsync(
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default);

    public Task<ReportDetailResponse?> GetReportDetailAsync(Guid reportId, CancellationToken ct = default);

    public Task<List<IntegrationResponse>?> GetIntegrationsAsync(CancellationToken ct = default);

    public Task<bool> VerifyEmailAsync(string token, CancellationToken ct = default);

    public Task<CheckoutSessionResponse?> CreateCheckoutSessionAsync(
        string plan,
        CancellationToken ct = default);

    public Task<BillingPortalSessionResponse?> CreateBillingPortalSessionAsync(
        CancellationToken ct = default);
}
