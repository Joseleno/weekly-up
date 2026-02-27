using Mediator;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

using WeeklyUp.Application.Common.Notifications;
using WeeklyUp.Domain.Common;

namespace WeeklyUp.Infrastructure.Persistence.Interceptors;

public sealed class DomainEventDispatchInterceptor : SaveChangesInterceptor
{
    private readonly IMediator _mediator;
    private readonly ILogger<DomainEventDispatchInterceptor> _logger;

    public DomainEventDispatchInterceptor(
        IMediator mediator,
        ILogger<DomainEventDispatchInterceptor> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            await DispatchDomainEventsAsync(eventData.Context, cancellationToken);
        }

        return result;
    }

    private async Task DispatchDomainEventsAsync(DbContext context, CancellationToken ct)
    {
        var aggregates = context.ChangeTracker
            .Entries<AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count != 0)
            .Select(e => e.Entity)
            .ToList();

        var events = aggregates.SelectMany(a => a.DomainEvents).ToList();

        foreach (IDomainEvent domainEvent in events)
        {
            try
            {
                await PublishEventAsync(domainEvent, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
#pragma warning disable CA1031 // Intencional: dispatcher absorve falhas de handlers individuais para garantir publicação de todos os eventos
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao publicar domain event {EventType} — EventId: {EventId}",
                    domainEvent.GetType().Name, domainEvent.EventId);
            }
#pragma warning restore CA1031
        }

        aggregates.ForEach(a => a.ClearDomainEvents());
    }

    private async Task PublishEventAsync(IDomainEvent domainEvent, CancellationToken ct)
    {
        var notification = new DomainEventNotification(domainEvent);
        await _mediator.Publish(notification, ct);
    }
}
