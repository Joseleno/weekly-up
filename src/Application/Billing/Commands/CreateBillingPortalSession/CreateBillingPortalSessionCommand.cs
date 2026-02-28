using Mediator;

using WeeklyUp.Application.Common.DTOs;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Billing.Commands.CreateBillingPortalSession;

public sealed record CreateBillingPortalSessionCommand(Guid UserId, string ReturnUrl)
    : ICommand<Result<BillingPortalSessionDto>>;
