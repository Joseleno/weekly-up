using Microsoft.Extensions.Options;

using Stripe;

using WeeklyUp.Application.Billing;
using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Shared.Results;

using BillingPortalSessionService = Stripe.BillingPortal.SessionService;
using CheckoutSessionService = Stripe.Checkout.SessionService;
using CustomerService = Stripe.CustomerService;

namespace WeeklyUp.Infrastructure.Billing;

public sealed class StripeService : IStripeService
{
    private readonly StripeOptions _options;
    private readonly CustomerService _customerService;
    private readonly CheckoutSessionService _checkoutSessionService;
    private readonly BillingPortalSessionService _portalSessionService;

    public StripeService(
        IOptions<StripeOptions> options,
        CustomerService customerService,
        CheckoutSessionService checkoutSessionService,
        BillingPortalSessionService portalSessionService)
    {
        _options = options.Value;
        _customerService = customerService;
        _checkoutSessionService = checkoutSessionService;
        _portalSessionService = portalSessionService;
    }

    public async Task<string> GetOrCreateCustomerAsync(string email, string name, CancellationToken ct = default)
    {
        var listOptions = new CustomerListOptions { Email = email, Limit = 1 };
        StripeList<Customer> existing = await _customerService.ListAsync(listOptions, null, ct);
        if (existing.Data.Count > 0)
        {
            return existing.Data[0].Id;
        }

        var createOptions = new CustomerCreateOptions { Email = email, Name = name };
        Customer customer = await _customerService.CreateAsync(createOptions, null, ct);
        return customer.Id;
    }

    public async Task<string> CreateCheckoutSessionAsync(
        string customerId,
        string priceId,
        string successUrl,
        string cancelUrl,
        Guid userId,
        CancellationToken ct = default)
    {
        var options = new Stripe.Checkout.SessionCreateOptions
        {
            Customer = customerId,
            Mode = "subscription",
            LineItems =
            [
                new() { Price = priceId, Quantity = 1 },
            ],
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            Metadata = new Dictionary<string, string>
            {
                ["userId"] = userId.ToString(),
            },
        };

        Stripe.Checkout.Session session = await _checkoutSessionService.CreateAsync(options, null, ct);
        return session.Url;
    }

    public async Task<string> CreateBillingPortalSessionAsync(
        string customerId,
        string returnUrl,
        CancellationToken ct = default)
    {
        var options = new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = customerId,
            ReturnUrl = returnUrl,
        };

        Stripe.BillingPortal.Session session = await _portalSessionService.CreateAsync(options, null, ct);
        return session.Url;
    }

    public Result<StripeWebhookEvent> ParseWebhookEvent(string payload, string signature)
    {
        try
        {
            Event stripeEvent = EventUtility.ConstructEvent(payload, signature, _options.WebhookSecret);

            string? userId = null;
            string? priceId = null;

            if (stripeEvent.Data.Object is Subscription subscription)
            {
                subscription.Metadata.TryGetValue("userId", out userId);
                priceId = subscription.Items?.Data?.FirstOrDefault()?.Price?.Id;
            }

            return new StripeWebhookEvent(stripeEvent.Type, stripeEvent.Id, userId, priceId);
        }
        catch (StripeException)
        {
            return AppError.Validation("Webhook.InvalidSignature", "Assinatura do webhook invalida.");
        }
    }
}
