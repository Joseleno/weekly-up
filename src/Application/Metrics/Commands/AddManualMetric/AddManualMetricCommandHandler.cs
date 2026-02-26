using Mediator;

using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Interfaces;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Metrics.Commands.AddManualMetric;

public sealed class AddManualMetricCommandHandler
    : ICommandHandler<AddManualMetricCommand, Result<bool>>
{
    private readonly IUnitOfWork _uow;

    public AddManualMetricCommandHandler(IUnitOfWork uow) => _uow = uow;

    public async ValueTask<Result<bool>> Handle(
        AddManualMetricCommand command,
        CancellationToken cancellationToken)
    {
        ManualMetric? existing = await _uow.ManualMetrics.GetByUserAndWeekAsync(
            command.UserId, command.WeekStart, cancellationToken);

        if (existing is not null)
        {
            Result<bool> updateResult = existing.Update(
                command.Revenue, command.SalesCount, command.NewCustomers, command.Visits);

            if (updateResult.IsFailure)
            {
                return updateResult.Error;
            }

            _uow.ManualMetrics.Update(existing);
        }
        else
        {
            Result<ManualMetric> createResult = ManualMetric.Create(
                command.UserId, command.WeekStart,
                command.Revenue, command.SalesCount, command.NewCustomers, command.Visits);

            if (createResult.IsFailure)
            {
                return createResult.Error;
            }

            await _uow.ManualMetrics.AddAsync(createResult.Value, cancellationToken);
        }

        return true;
    }
}
