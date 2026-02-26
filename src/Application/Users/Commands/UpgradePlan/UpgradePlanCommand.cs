using Mediator;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Users.Commands.UpgradePlan;

public sealed record UpgradePlanCommand(Guid UserId, PlanType NewPlan) : ICommand<Result<bool>>;
