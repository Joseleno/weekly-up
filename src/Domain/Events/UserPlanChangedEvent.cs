using WeeklyUp.Domain.Common;
using WeeklyUp.Domain.Enums;

namespace WeeklyUp.Domain.Events;

public sealed record UserPlanChangedEvent(Guid UserId, PlanType PreviousPlan, PlanType NewPlan) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
