using WeeklyUp.Application.Billing;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Common.Interfaces;

public interface IStripeService
{
    public Task<string> GetOrCreateCustomerAsync(string email, string name, CancellationToken ct = default);
    public Task<string> CreateCheckoutSessionAsync(
        string customerId,
        string priceId,
        string successUrl,
        string cancelUrl,
        Guid userId,
        CancellationToken ct = default);
    public Task<string> CreateBillingPortalSessionAsync(string customerId, string returnUrl, CancellationToken ct = default);
    public Result<StripeWebhookEvent> ParseWebhookEvent(string payload, string signature);
}
