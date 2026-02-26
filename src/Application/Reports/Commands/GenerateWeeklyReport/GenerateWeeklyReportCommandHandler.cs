using Mediator;

using WeeklyUp.Shared.Results;

namespace WeeklyUp.Application.Reports.Commands.GenerateWeeklyReport;

/// <summary>
/// Stub handler — implementação completa na Fase 5 (Api) ou futura iteração.
/// Responsável por orquestrar: busca de dados, geração de métricas, insights e envio de relatório.
/// </summary>
public sealed class GenerateWeeklyReportCommandHandler
    : ICommandHandler<GenerateWeeklyReportCommand, Result<bool>>
{
    public ValueTask<Result<bool>> Handle(
        GenerateWeeklyReportCommand command,
        CancellationToken cancellationToken)
    {
        // TODO: Implementar geração de relatório com DataAggregator, ClaudeInsightGenerator, etc.
        return ValueTask.FromResult(Result.Success(true));
    }
}
