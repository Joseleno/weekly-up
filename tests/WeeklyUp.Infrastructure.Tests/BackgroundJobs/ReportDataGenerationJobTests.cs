using FluentAssertions;

using Microsoft.Extensions.Logging;

using NSubstitute;

using WeeklyUp.Application.Common.Interfaces;
using WeeklyUp.Domain.Entities;
using WeeklyUp.Domain.Enums;
using WeeklyUp.Domain.Interfaces;
using WeeklyUp.Domain.Interfaces.Repositories;
using WeeklyUp.Domain.Interfaces.Services;
using WeeklyUp.Domain.ValueObjects;
using WeeklyUp.Infrastructure.BackgroundJobs;
using WeeklyUp.Shared.Results;

namespace WeeklyUp.Infrastructure.Tests.BackgroundJobs;

public sealed class ReportDataGenerationJobTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IDataAggregator _dataAggregator = Substitute.For<IDataAggregator>();
    private readonly IInsightGenerator _insightGenerator = Substitute.For<IInsightGenerator>();
    private readonly IDataSourceProvider _ga4Provider = Substitute.For<IDataSourceProvider>();
    private readonly IReportJobScheduler _jobScheduler = Substitute.For<IReportJobScheduler>();
    private readonly ILogger<ReportDataGenerationJob> _logger = Substitute.For<ILogger<ReportDataGenerationJob>>();
    private readonly IReportRepository _reports = Substitute.For<IReportRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IIntegrationRepository _integrations = Substitute.For<IIntegrationRepository>();

    public ReportDataGenerationJobTests()
    {
        _uow.Reports.Returns(_reports);
        _uow.Users.Returns(_users);
        _uow.Integrations.Returns(_integrations);
        _ga4Provider.ProviderType.Returns(IntegrationProvider.GoogleAnalytics4);
        _integrations.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<Integration>().AsReadOnly());
    }

    private ReportDataGenerationJob CreateJob(IEnumerable<IDataSourceProvider>? providers = null) =>
        new(_uow, _dataAggregator, _insightGenerator, providers ?? [_ga4Provider], _jobScheduler, _logger);

    private static Report CreateReport(Guid? userId = null)
    {
        var weekRange = DateRange.Create(
            new DateOnly(2026, 2, 16),
            new DateOnly(2026, 2, 22)).Value;
        return Report.Create(userId ?? Guid.NewGuid(), weekRange);
    }

    private static User CreateUser()
    {
        var result = User.Create("user@test.com", "Test User", "My Business", BusinessType.Ecommerce, verificationToken: "test-token");
        return result.Value;
    }

    private static ReportMetrics CreateMetrics() =>
        new(Money.BRL(5000m), 100, Money.BRL(50m), 20, 500, 400, 1000, "/home", "google", null, null);

    private static ReportInsights CreateInsights() =>
        new("Faturamento cresceu 10%", "Queda em visitas mobile", "Investir em SEO",
            new DateTime(2026, 2, 16, 0, 0, 0, DateTimeKind.Utc));

    private static Integration CreateGa4Integration()
    {
        var encryptor = Substitute.For<ITokenEncryptor>();
        encryptor.Encrypt(Arg.Any<string>()).Returns("encrypted-token");
        var userResult = User.Create("ga4owner@test.com", "GA4 Owner", "GA4 Business", BusinessType.Ecommerce, verificationToken: "test-token");
        var user = userResult.Value;
        var integrationResult = user.AddIntegration(
            IntegrationProvider.GoogleAnalytics4,
            "access-token",
            "refresh-token",
            "ga4-account-id",
            "ga4-property-id",
            encryptor);
        return integrationResult.Value;
    }

    [Fact]
    public async Task ExecuteAsync_WhenReportNotFound_ShouldLogWarningAndReturn()
    {
        // Arrange
        var job = CreateJob();
        var reportId = Guid.NewGuid();
        _reports.GetByIdAsync(reportId, Arg.Any<CancellationToken>()).Returns((Report?)null);

        // Act
        await job.ExecuteAsync(reportId);

        // Assert
        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());

        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserNotFound_ShouldMarkFailedAndSave()
    {
        // Arrange
        var job = CreateJob();
        var report = CreateReport();
        _reports.GetByIdAsync(report.Id, Arg.Any<CancellationToken>()).Returns(report);
        _users.GetByIdAsync(report.UserId, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        await job.ExecuteAsync(report.Id);

        // Assert
        report.Status.Should().Be(ReportStatus.Failed);
        report.FailureReason.Should().NotBeNullOrWhiteSpace();
        _reports.Received(1).Update(report);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _jobScheduler.DidNotReceive().ScheduleReportSending(Arg.Any<Guid>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenDataAggregatorFails_ShouldMarkReportFailedAndSave()
    {
        // Arrange
        var job = CreateJob();
        var user = CreateUser();
        var report = CreateReport(user.Id);
        _reports.GetByIdAsync(report.Id, Arg.Any<CancellationToken>()).Returns(report);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _dataAggregator.GetAggregatedMetricsAsync(user.Id, report.WeekRange, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<ReportMetrics>(AppError.Failure("Aggregator.Failed", "Erro ao coletar dados")));

        // Act
        await job.ExecuteAsync(report.Id);

        // Assert
        report.Status.Should().Be(ReportStatus.Failed);
        report.FailureReason.Should().NotBeNullOrWhiteSpace();
        _reports.Received(1).Update(report);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _jobScheduler.DidNotReceive().ScheduleReportSending(Arg.Any<Guid>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoGa4Integration_ShouldContinueWithoutDemographics()
    {
        // Arrange
        var job = CreateJob();
        var user = CreateUser();
        var report = CreateReport(user.Id);
        var metrics = CreateMetrics();
        var insights = CreateInsights();

        _reports.GetByIdAsync(report.Id, Arg.Any<CancellationToken>()).Returns(report);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _dataAggregator.GetAggregatedMetricsAsync(user.Id, report.WeekRange, Arg.Any<CancellationToken>())
            .Returns(Result.Success(metrics));
        _insightGenerator.GenerateAsync(metrics, user.BusinessType, "pt-BR", Arg.Any<CancellationToken>())
            .Returns(Result.Success(insights));

        // Act
        await job.ExecuteAsync(report.Id);

        // Assert
        report.Status.Should().Be(ReportStatus.Generated);
        report.Demographics.Should().BeNull();
        report.Insights.Should().NotBeNull();
        _jobScheduler.Received(1).ScheduleReportSending(report.Id);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAllSucceeds_ShouldMarkGeneratedAddInsightsAndScheduleSending()
    {
        // Arrange
        var job = CreateJob();
        var user = CreateUser();
        var report = CreateReport(user.Id);
        var metrics = CreateMetrics();
        var insights = CreateInsights();

        _reports.GetByIdAsync(report.Id, Arg.Any<CancellationToken>()).Returns(report);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _dataAggregator.GetAggregatedMetricsAsync(user.Id, report.WeekRange, Arg.Any<CancellationToken>())
            .Returns(Result.Success(metrics));
        _insightGenerator.GenerateAsync(metrics, user.BusinessType, "pt-BR", Arg.Any<CancellationToken>())
            .Returns(Result.Success(insights));

        // Act
        await job.ExecuteAsync(report.Id);

        // Assert
        report.Status.Should().Be(ReportStatus.Generated);
        report.Metrics.Should().Be(metrics);
        report.Insights.Should().Be(insights);
        _reports.Received(1).Update(report);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _jobScheduler.Received(1).ScheduleReportSending(report.Id);
    }

    [Fact]
    public async Task ExecuteAsync_WhenGa4DemographicsCallFails_ShouldLogWarningAndContinue()
    {
        // Arrange
        var job = CreateJob();
        var user = CreateUser();
        var report = CreateReport(user.Id);
        var metrics = CreateMetrics();
        var insights = CreateInsights();
        var ga4Integration = CreateGa4Integration();

        _reports.GetByIdAsync(report.Id, Arg.Any<CancellationToken>()).Returns(report);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _dataAggregator.GetAggregatedMetricsAsync(user.Id, report.WeekRange, Arg.Any<CancellationToken>())
            .Returns(Result.Success(metrics));
        _integrations.GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Integration> { ga4Integration }.AsReadOnly());
        _ga4Provider.GetDemographicsAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string?>(),
            Arg.Any<DateRange>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<Demographics?>(AppError.Failure("GA4.Failed", "API timeout")));
        _insightGenerator.GenerateAsync(metrics, user.BusinessType, "pt-BR", Arg.Any<CancellationToken>())
            .Returns(Result.Success(insights));

        // Act
        await job.ExecuteAsync(report.Id);

        // Assert
        report.Status.Should().Be(ReportStatus.Generated);
        report.Demographics.Should().BeNull();
        report.Insights.Should().NotBeNull();
        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
        _jobScheduler.Received(1).ScheduleReportSending(report.Id);
    }

    [Fact]
    public async Task ExecuteAsync_WhenInsightGeneratorFails_ShouldSaveAndScheduleSendingWithoutInsights()
    {
        // Arrange
        var job = CreateJob();
        var user = CreateUser();
        var report = CreateReport(user.Id);
        var metrics = CreateMetrics();

        _reports.GetByIdAsync(report.Id, Arg.Any<CancellationToken>()).Returns(report);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _dataAggregator.GetAggregatedMetricsAsync(user.Id, report.WeekRange, Arg.Any<CancellationToken>())
            .Returns(Result.Success(metrics));
        _insightGenerator.GenerateAsync(Arg.Any<ReportMetrics>(), Arg.Any<BusinessType>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<ReportInsights>(AppError.Failure("AI.Failed", "Timeout")));

        // Act
        await job.ExecuteAsync(report.Id);

        // Assert
        report.Status.Should().Be(ReportStatus.Generated);
        report.Insights.Should().BeNull();
        _reports.Received(1).Update(report);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _jobScheduler.Received(1).ScheduleReportSending(report.Id);
    }

    [Fact]
    public async Task ExecuteAsync_WhenMarkGeneratedFails_ShouldLogErrorAndReturn()
    {
        // Arrange
        var job = CreateJob();
        var user = CreateUser();
        var report = CreateReport(user.Id);
        report.MarkFailed("pre-existing failure"); // Status = Failed — MarkGenerated will reject this
        var metrics = CreateMetrics();

        _reports.GetByIdAsync(report.Id, Arg.Any<CancellationToken>()).Returns(report);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _dataAggregator.GetAggregatedMetricsAsync(user.Id, report.WeekRange, Arg.Any<CancellationToken>())
            .Returns(Result.Success(metrics));

        // Act
        await job.ExecuteAsync(report.Id);

        // Assert
        _logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        _jobScheduler.DidNotReceive().ScheduleReportSending(Arg.Any<Guid>());
    }
}
