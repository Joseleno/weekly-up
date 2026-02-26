using FluentAssertions;

using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.Events;
using WeeklyUp.Domain.ValueObjects;

namespace WeeklyUp.Domain.Testes.Entities;

public sealed class ReportTests
{
    private static DateRange CreateWeekRange() =>
        DateRange.Create(new DateOnly(2025, 1, 6), new DateOnly(2025, 1, 12)).Value;

    private static ReportMetrics CreateMetrics() =>
        new(Money.BRL(1000m), 10, Money.BRL(100m), 5, 200, 150, 400, "/home", "Google", null, null);

    [Fact]
    public void Create_ValidData_SetsPendingStatusAndRaisesEvent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var range = CreateWeekRange();

        // Act
        var report = Report.Create(userId, range);

        // Assert
        report.UserId.Should().Be(userId);
        report.Status.Should().Be(ReportStatus.Pending);
        report.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<ReportGeneratedEvent>();
    }

    [Fact]
    public void MarkGenerated_FromPending_SetsGeneratedStatus()
    {
        // Arrange
        var report = Report.Create(Guid.NewGuid(), CreateWeekRange());
        report.ClearDomainEvents();
        var metrics = CreateMetrics();

        // Act
        var result = report.MarkGenerated(metrics);

        // Assert
        result.IsSuccess.Should().BeTrue();
        report.Status.Should().Be(ReportStatus.Generated);
        report.Metrics.Should().Be(metrics);
    }

    [Fact]
    public void MarkGenerated_FromSent_ReturnsFailure()
    {
        // Arrange
        var report = Report.Create(Guid.NewGuid(), CreateWeekRange());
        report.MarkGenerated(CreateMetrics());
        report.MarkSent();

        // Act
        var result = report.MarkGenerated(CreateMetrics());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Report.InvalidStatus");
    }

    [Fact]
    public void MarkEmailSent_SetsTimestampAndRaisesEvent()
    {
        // Arrange
        var report = Report.Create(Guid.NewGuid(), CreateWeekRange());
        report.ClearDomainEvents();

        // Act
        report.MarkEmailSent();

        // Assert
        report.EmailSentAt.Should().NotBeNull();
        report.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ReportSentEvent>()
            .Which.Channel.Should().Be("Email");
    }

    [Fact]
    public void MarkWhatsAppSent_SetsTimestampAndRaisesEvent()
    {
        // Arrange
        var report = Report.Create(Guid.NewGuid(), CreateWeekRange());
        report.ClearDomainEvents();

        // Act
        report.MarkWhatsAppSent();

        // Assert
        report.WhatsAppSentAt.Should().NotBeNull();
        report.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ReportSentEvent>()
            .Which.Channel.Should().Be("WhatsApp");
    }

    [Fact]
    public void AddInsights_SetsInsights()
    {
        // Arrange
        var report = Report.Create(Guid.NewGuid(), CreateWeekRange());
        var insights = new ReportInsights("Highlight", "Alert", "Tip", DateTime.UtcNow);

        // Act
        report.AddInsights(insights);

        // Assert
        report.Insights.Should().Be(insights);
    }

    [Fact]
    public void MarkGenerated_FromFailed_ReturnsFailure()
    {
        // Arrange
        var report = Report.Create(Guid.NewGuid(), CreateWeekRange());
        report.MarkFailed("Erro simulado.");

        // Act
        var result = report.MarkGenerated(CreateMetrics());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Report.InvalidStatus");
    }

    [Fact]
    public void MarkFailed_SetsFailedStatus()
    {
        // Arrange
        var report = Report.Create(Guid.NewGuid(), CreateWeekRange());

        // Act
        report.MarkFailed("Erro ao gerar relatorio.");

        // Assert
        report.Status.Should().Be(ReportStatus.Failed);
    }
}
