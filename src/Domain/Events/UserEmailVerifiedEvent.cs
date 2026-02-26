using WeeklyUp.Domain.Common;

namespace WeeklyUp.Domain.Events;

public sealed record UserEmailVerifiedEvent(Guid UserId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
