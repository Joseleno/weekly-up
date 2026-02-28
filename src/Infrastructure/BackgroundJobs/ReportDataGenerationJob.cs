using Microsoft.Extensions.Logging;

using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.Interfaces;

namespace WeeklyUp.Infrastructure.BackgroundJobs;

public sealed class ReportDataGenerationJob
{
    private const string DefaultLanguage = "pt-BR";

    private readonly IUnitOfWork _uow;
    private readonly IDataAggregator _dataAggregator;
    private readonly IInsightGenerator _insightGenerator;
    private readonly IEnumerable<IDataSourceProvider> _providers;
    private readonly IReportJobScheduler _jobScheduler;
    private readonly ILogger<ReportDataGenerationJob> _logger;

    public ReportDataGenerationJob(
        IUnitOfWork uow,
        IDataAggregator dataAggregator,
        IInsightGenerator insightGenerator,
        IEnumerable<IDataSourceProvider> providers,
        IReportJobScheduler jobScheduler,
        ILogger<ReportDataGenerationJob> logger)
    {
        _uow = uow;
        _dataAggregator = dataAggregator;
        _insightGenerator = insightGenerator;
        _providers = providers;
        _jobScheduler = jobScheduler;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid reportId, CancellationToken ct = default)
    {
        var report = await _uow.Reports.GetByIdAsync(reportId, ct);
        if (report is null)
        {
            _logger.LogWarning("Report {ReportId} nao encontrado para geracao de dados", reportId);
            return;
        }

        var user = await _uow.Users.GetByIdAsync(report.UserId, ct);
        if (user is null)
        {
            _logger.LogWarning("Usuario {UserId} nao encontrado para relatorio {ReportId}", report.UserId, reportId);
            report.MarkFailed("Usuario nao encontrado.");
            _uow.Reports.Update(report);
            await _uow.SaveChangesAsync(ct);
            return;
        }

        var metricsResult = await _dataAggregator.GetAggregatedMetricsAsync(user.Id, report.WeekRange, ct);
        if (metricsResult.IsFailure)
        {
            _logger.LogError("Falha ao agregar metricas para relatorio {ReportId}: {Error}", reportId, metricsResult.Error);
            report.MarkFailed(metricsResult.Error.Message);
            _uow.Reports.Update(report);
            await _uow.SaveChangesAsync(ct);
            return;
        }

        var markResult = report.MarkGenerated(metricsResult.Value);
        if (markResult.IsFailure)
        {
            _logger.LogError(
                "Falha ao marcar relatorio {ReportId} como gerado: {Error}",
                reportId, markResult.Error);
            return;
        }

        await TrySetDemographicsAsync(report, user.Id, ct);

        var insightsResult = await _insightGenerator.GenerateAsync(
            metricsResult.Value, user.BusinessType, DefaultLanguage, ct);

        if (insightsResult.IsSuccess)
        {
            report.AddInsights(insightsResult.Value);
        }
        else
        {
            _logger.LogWarning(
                "InsightGenerator falhou para relatorio {ReportId}: {Error}. Relatorio sera enviado sem insights.",
                reportId, insightsResult.Error);
        }

        _uow.Reports.Update(report);
        await _uow.SaveChangesAsync(ct);

        _jobScheduler.ScheduleReportSending(report.Id);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Relatorio {ReportId} gerado com sucesso — insights: {HasInsights}, demographics: {HasDemographics}",
                reportId, report.Insights is not null, report.Demographics is not null);
        }
    }

    private async Task TrySetDemographicsAsync(
        Report report, Guid userId, CancellationToken ct)
    {
        var ga4Provider = _providers.FirstOrDefault(p => p.ProviderType == IntegrationProvider.GoogleAnalytics4);
        if (ga4Provider is null)
        {
            return;
        }

        var integrations = await _uow.Integrations.GetByUserIdAsync(userId, ct);
        var ga4Integration = integrations.FirstOrDefault(
            i => i.Provider == IntegrationProvider.GoogleAnalytics4
              && i.Status == IntegrationStatus.Connected);

        if (ga4Integration is null)
        {
            return;
        }

        var demoResult = await ga4Provider.GetDemographicsAsync(
            userId,
            ga4Integration.ProviderAccountId,
            ga4Integration.PropertyId,
            report.WeekRange,
            ct);

        if (demoResult.IsSuccess && demoResult.Value is not null)
        {
            report.SetDemographics(demoResult.Value);
        }
        else if (demoResult.IsFailure)
        {
            _logger.LogWarning(
                "Demographics GA4 falhou para relatorio {ReportId}: {Error}",
                report.Id,
                demoResult.Error);
        }
    }
}
