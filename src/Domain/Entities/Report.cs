using WeeklyUp.Domain.Common;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.Events;
using WeeklyUp.Domain.ValueObjects;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Domain.Entities;

public sealed class Report : AggregateRoot
{
    private const string EmailChannel = "Email";
    private const string WhatsAppChannel = "WhatsApp";

    public Guid UserId { get; private set; }
    public DateRange WeekRange { get; private set; } = null!;
    public ReportStatus Status { get; private set; }
    public ReportMetrics? Metrics { get; private set; }
    public Demographics? Demographics { get; private set; }
    public ReportInsights? Insights { get; private set; }
    public DateTime? EmailSentAt { get; private set; }
    public DateTime? WhatsAppSentAt { get; private set; }
    public string? FailureReason { get; private set; }

    private Report() { }

    public static Report Create(Guid userId, DateRange weekRange)
    {
        ArgumentNullException.ThrowIfNull(weekRange);

        Report report = new()
        {
            UserId = userId,
            WeekRange = weekRange,
            Status = ReportStatus.Pending,
        };

        report.RaiseDomainEvent(new ReportGeneratedEvent(report.Id, userId, weekRange));
        return report;
    }

    public Result<bool> MarkGenerated(ReportMetrics metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);

        if (Status != ReportStatus.Pending && Status != ReportStatus.Generating)
        {
            return AppError.Validation(
                "Report.InvalidStatus",
                $"Report com status {Status} nao pode ser marcado como gerado.");
        }

        Metrics = metrics;
        Status = ReportStatus.Generated;
        UpdatedAt = DateTime.UtcNow;
        return true;
    }

    public void AddInsights(ReportInsights insights)
    {
        ArgumentNullException.ThrowIfNull(insights);
        Insights = insights;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDemographics(Demographics demographics)
    {
        ArgumentNullException.ThrowIfNull(demographics);
        Demographics = demographics;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkEmailSent()
    {
        EmailSentAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ReportSentEvent(Id, UserId, EmailChannel));
    }

    public void MarkWhatsAppSent()
    {
        WhatsAppSentAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new ReportSentEvent(Id, UserId, WhatsAppChannel));
    }

    public void MarkSent()
    {
        Status = ReportStatus.Sent;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        Status = ReportStatus.Failed;
        FailureReason = error;
        UpdatedAt = DateTime.UtcNow;
    }
}
