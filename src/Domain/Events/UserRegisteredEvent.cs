using WeeklyUp.Domain.Common;

namespace WeeklyUp.Domain.Events;

public sealed record UserRegisteredEvent(
    Guid UserId,
    string Email,
    string Name,
    string? VerificationToken) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
