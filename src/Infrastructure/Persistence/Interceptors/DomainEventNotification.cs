using Mediator;

using WeeklyUp.Domain.Common;

namespace WeeklyUp.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Wrapper não-genérico que adapta um IDomainEvent para INotification do Mediator,
/// permitindo publicação via IMediator.Publish sem que o Domain referencie Mediator.
/// </summary>
public sealed record DomainEventNotification(IDomainEvent Event) : INotification;
