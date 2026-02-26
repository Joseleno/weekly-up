using Mediator;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Metrics.Commands.AddManualMetric;

public sealed record AddManualMetricCommand(
    Guid UserId,
    DateOnly WeekStart,
    decimal? Revenue,
    int? SalesCount,
    int? NewCustomers,
    int? Visits) : ICommand<Result<bool>>;
