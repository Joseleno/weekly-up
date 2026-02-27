using Mediator;

using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Billing.Commands.CreateCheckoutSession;

public sealed record CreateCheckoutSessionCommand(
    Guid UserId,
    PlanType Plan,
    string SuccessUrl,
    string CancelUrl) : ICommand<Result<CheckoutSessionDto>>;
