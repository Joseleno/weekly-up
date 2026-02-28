using Carter;

using Mediator;

using WeeklyUp.Api.Extensions;
using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Application.Reports.Commands.GenerateWeeklyReport;
using WeeklyUp.Application.Reports.Commands.UpdateReportPreferences;
using WeeklyUp.Application.Reports.Queries.GetReportDetail;
using WeeklyUp.Application.Reports.Queries.GetReportHistory;
using WeeklyUp.Application.Reports.Queries.GetReportPreferences;
using WeeklyUp.Domain.Enums;

namespace WeeklyUp.Api.Modules;

public sealed record UpdatePreferencesRequest(
    DayOfWeekPreference SendDay,
    TimeOnly SendTime,
    IReadOnlyList<string> EnabledSections);

public sealed class ReportsModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/reports")
            .WithTags("Reports")
            .RequireAuthorization()
            .RequireRateLimiting("api");

        group.MapGet("/", async (
            IMediator mediator,
            ICurrentUserService currentUser,
            int page,
            int pageSize,
            CancellationToken ct) =>
        {
            var query = new GetReportHistoryQuery(currentUser.UserId, page, pageSize);
            var result = await mediator.Send(query, ct);
            return result.Match(
                dto => Results.Ok(dto),
                error => error.ToProblem());
        })
        .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/{id:guid}", async (
            Guid id,
            IMediator mediator,
            ICurrentUserService currentUser,
            CancellationToken ct) =>
        {
            var query = new GetReportDetailQuery(currentUser.UserId, id);
            var result = await mediator.Send(query, ct);
            return result.Match(
                dto => Results.Ok(dto),
                error => error.ToProblem());
        })
        .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/preferences", async (
            IMediator mediator,
            ICurrentUserService currentUser,
            CancellationToken ct) =>
        {
            var query = new GetReportPreferencesQuery(currentUser.UserId);
            var result = await mediator.Send(query, ct);
            return result.Match(
                dto => Results.Ok(dto),
                error => error.ToProblem());
        })
        .Produces(StatusCodes.Status401Unauthorized);

        group.MapPut("/preferences", async (
            UpdatePreferencesRequest request,
            IMediator mediator,
            ICurrentUserService currentUser,
            CancellationToken ct) =>
        {
            var command = new UpdateReportPreferencesCommand(
                currentUser.UserId,
                request.SendDay,
                request.SendTime,
                request.EnabledSections);
            var result = await mediator.Send(command, ct);
            return result.Match(
                _ => Results.NoContent(),
                error => error.ToProblem());
        })
        .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/generate", async (
            IMediator mediator,
            ICurrentUserService currentUser,
            CancellationToken ct) =>
        {
            var command = new GenerateWeeklyReportCommand(currentUser.UserId);
            var result = await mediator.Send(command, ct);
            return result.Match(
                _ => Results.Accepted(),
                error => error.ToProblem());
        })
        .Produces(StatusCodes.Status401Unauthorized);
    }
}
