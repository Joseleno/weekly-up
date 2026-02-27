using Microsoft.Extensions.Logging;

using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Domain.Interfaces;
using WeeklyUp.Domain.Interfaces.Services;
using WeeklyUp.Shared.Constants;

namespace WeeklyUp.Infrastructure.BackgroundJobs;

public sealed class ReportSendingJob
{
    private readonly IEmailSender _emailSender;
    private readonly IWhatsAppSender _whatsAppSender;
    private readonly IUnitOfWork _uow;
    private readonly IApplicationCacheService _cache;
    private readonly ILogger<ReportSendingJob> _logger;

    public ReportSendingJob(
        IEmailSender emailSender,
        IWhatsAppSender whatsAppSender,
        IUnitOfWork uow,
        IApplicationCacheService cache,
        ILogger<ReportSendingJob> logger)
    {
        _emailSender = emailSender;
        _whatsAppSender = whatsAppSender;
        _uow = uow;
        _cache = cache;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid reportId, CancellationToken ct = default)
    {
        var report = await _uow.Reports.GetByIdAsync(reportId, ct);
        if (report is null)
        {
            _logger.LogWarning("Report {ReportId} nao encontrado para envio", reportId);
            return;
        }

        var user = await _uow.Users.GetByIdAsync(report.UserId, ct);
        if (user is null)
        {
            _logger.LogWarning("Usuario {UserId} nao encontrado para envio do relatorio {ReportId}", report.UserId, reportId);
            return;
        }

        var emailResult = await _emailSender.SendWeeklyReportAsync(user.Email.Value, user.Name, report, ct);
        if (!emailResult.IsSuccess)
        {
            _logger.LogError("Falha ao enviar email para usuario {UserId}: {Error}", report.UserId, emailResult.Error);
            return;
        }

        report.MarkEmailSent();

        if (user.CanAccessWhatsApp() && user.PhoneNumber is not null)
        {
            var whatsAppResult = await _whatsAppSender.SendWeeklyReportAsync(user.PhoneNumber, user.Name, report, ct);
            if (whatsAppResult.IsSuccess)
            {
                report.MarkWhatsAppSent();
            }
            else
            {
                _logger.LogWarning("WhatsApp falhou para usuario {UserId}: {Error}", report.UserId, whatsAppResult.Error);
            }
        }

        report.MarkSent();
        _uow.Reports.Update(report);
        await _uow.SaveChangesAsync(ct);

        await _cache.RemoveAsync(CacheKeys.Report(report.Id), ct);
        await _cache.RemoveAsync(CacheKeys.Dashboard(report.UserId), ct);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Relatorio {ReportId} enviado com sucesso", reportId);
        }
    }
}
