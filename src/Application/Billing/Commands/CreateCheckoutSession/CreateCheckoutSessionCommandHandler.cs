using Mediator;

using Microsoft.Extensions.Options;

using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Application.Common.Settings;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.Interfaces;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Billing.Commands.CreateCheckoutSession;

public sealed class CreateCheckoutSessionCommandHandler
    : ICommandHandler<CreateCheckoutSessionCommand, Result<CheckoutSessionDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly IStripeService _stripeService;
    private readonly StripeSettings _stripeSettings;

    public CreateCheckoutSessionCommandHandler(
        IUnitOfWork uow,
        IStripeService stripeService,
        IOptions<StripeSettings> stripeSettings)
    {
        _uow = uow;
        _stripeService = stripeService;
        _stripeSettings = stripeSettings.Value;
    }

    public async ValueTask<Result<CheckoutSessionDto>> Handle(
        CreateCheckoutSessionCommand command,
        CancellationToken cancellationToken)
    {
        User? user = await _uow.Users.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return AppError.NotFound("User.NotFound", $"Usuario com Id '{command.UserId}' nao encontrado.");
        }

        string customerId = user.StripeCustomerId ?? await CreateAndPersistCustomerAsync(user, cancellationToken);

        string priceId = ResolvePriceId(command.Plan);
        if (string.IsNullOrWhiteSpace(priceId))
        {
            return AppError.Validation("Billing.InvalidPlan", $"Plano '{command.Plan}' sem preco configurado.");
        }

        string url = await _stripeService.CreateCheckoutSessionAsync(
            customerId, priceId, command.SuccessUrl, command.CancelUrl, command.UserId, cancellationToken);

        return new CheckoutSessionDto(url);
    }

    private async Task<string> CreateAndPersistCustomerAsync(User user, CancellationToken ct)
    {
        string customerId = await _stripeService.GetOrCreateCustomerAsync(user.Email.Value, user.Name, ct);
        user.SetStripeCustomerId(customerId);
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(ct);
        return customerId;
    }

    private string ResolvePriceId(PlanType plan) => plan switch
    {
        PlanType.Pro => _stripeSettings.ProPriceId,
        PlanType.Business => _stripeSettings.BusinessPriceId,
        _ => string.Empty,
    };
}
