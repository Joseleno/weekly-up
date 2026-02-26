using Carter;

using Mediator;

using WeeklyUp.Api.Extensions;
using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Application.Metrics.Commands.AddManualMetric;

namespace WeeklyUp.Api.Modules;

public sealed record AddManualMetricRequest(
    DateOnly WeekStart,
    decimal? Revenue,
    int? SalesCount,
    int? NewCustomers,
    int? Visits);

public sealed class MetricsModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/metrics")
            .WithTags("Metrics")
            .RequireAuthorization();

        group.MapPost("/", async (
            AddManualMetricRequest request,
            IMediator mediator,
            ICurrentUserService currentUser,
            CancellationToken ct) =>
        {
            var command = new AddManualMetricCommand(
                currentUser.UserId,
                request.WeekStart,
                request.Revenue,
                request.SalesCount,
                request.NewCustomers,
                request.Visits);
            var result = await mediator.Send(command, ct);
            return result.Match(
                _ => Results.NoContent(),
                error => error.ToProblem());
        });
    }
}
