using Mediator;

using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Interfaces;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Billing.Commands.CreateBillingPortalSession;

public sealed class CreateBillingPortalSessionCommandHandler
    : ICommandHandler<CreateBillingPortalSessionCommand, Result<BillingPortalSessionDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly IStripeService _stripeService;

    public CreateBillingPortalSessionCommandHandler(IUnitOfWork uow, IStripeService stripeService)
    {
        _uow = uow;
        _stripeService = stripeService;
    }

    public async ValueTask<Result<BillingPortalSessionDto>> Handle(
        CreateBillingPortalSessionCommand command,
        CancellationToken cancellationToken)
    {
        User? user = await _uow.Users.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return AppError.NotFound("User.NotFound", $"Usuario com Id '{command.UserId}' nao encontrado.");
        }

        if (string.IsNullOrWhiteSpace(user.StripeCustomerId))
        {
            return AppError.Validation("Billing.NoStripeCustomer", "Usuario nao possui assinatura ativa no Stripe.");
        }

        string url = await _stripeService.CreateBillingPortalSessionAsync(
            user.StripeCustomerId, command.ReturnUrl, cancellationToken);

        return new BillingPortalSessionDto(url);
    }
}
